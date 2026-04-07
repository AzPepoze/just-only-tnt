using System.Collections.Generic;
using Godot;
using justonlytnt.Networking;

namespace justonlytnt;

public sealed partial class GameBootstrap
{
    private readonly Dictionary<long, NetPlayerState> _playerStates = new();
    private readonly Dictionary<long, string> _playerNames = new();
    private readonly Dictionary<long, RemotePlayerAvatar> _remoteAvatars = new();
    private double _stateBroadcastTimer;

    private const double StateBroadcastSeconds = 0.05;

    private struct NetPlayerState
    {
        public Vector3 Position;
        public float Yaw;
        public float Pitch;

        public NetPlayerState(Vector3 position, float yaw, float pitch)
        {
            Position = position;
            Yaw = yaw;
            Pitch = pitch;
        }
    }

    private void SetupMultiplayer(LaunchMode mode)
    {
        if (!_networkEnabled)
        {
            return;
        }

        ENetMultiplayerPeer peer = new();
        Error err;

        if (mode is LaunchMode.Host or LaunchMode.DedicatedServer)
        {
            err = peer.CreateServer(LaunchOptions.Port, LaunchOptions.MaxPlayers);
            if (err != Error.Ok)
            {
                GD.PushError($"Failed to create server on port {LaunchOptions.Port}: {err}");
                _networkEnabled = false;
                return;
            }
        }
        else
        {
            err = peer.CreateClient(LaunchOptions.JoinAddress, LaunchOptions.Port);
            if (err != Error.Ok)
            {
                GD.PushError($"Failed to connect to {LaunchOptions.JoinAddress}:{LaunchOptions.Port}: {err}");
                _networkEnabled = false;
                return;
            }
        }

        Multiplayer.MultiplayerPeer = peer;
    }

    private void InitializeNetworkRuntime(LaunchMode mode)
    {
        if (!_networkEnabled)
        {
            return;
        }

        Multiplayer.PeerConnected += OnPeerConnected;
        Multiplayer.PeerDisconnected += OnPeerDisconnected;
        Multiplayer.ConnectedToServer += OnConnectedToServer;
        Multiplayer.ConnectionFailed += OnConnectionFailed;
        Multiplayer.ServerDisconnected += OnServerDisconnected;

	        if (Multiplayer.IsServer() && !_dedicatedServer)
	        {
	            long selfId = Multiplayer.GetUniqueId();
	            _playerNames[selfId] = LaunchOptions.PlayerName;
	            Vector3 spawn = _localPlayer?.GlobalPosition ?? new Vector3(0f, 42f, 0f);
	            float yaw = _localPlayer?.Rotation.Y ?? 0f;
            float pitch = _localPlayer?.ViewPitch ?? 0f;
            _playerStates[selfId] = new NetPlayerState(spawn, yaw, pitch);
        }

        if (mode == LaunchMode.Client)
        {
            GD.Print($"Connecting to {LaunchOptions.JoinAddress}:{LaunchOptions.Port} ...");
        }
    }

    public override void _Process(double delta)
    {
        if (!_networkEnabled)
        {
            return;
        }

        UpdateLocalPoseSync();

        if (!Multiplayer.IsServer())
        {
            return;
        }

        if (_dedicatedServer && _serverStreamTarget is not null)
        {
            foreach ((long _, NetPlayerState state) in _playerStates)
            {
                _serverStreamTarget.GlobalPosition = state.Position;
                break;
            }
        }

        _stateBroadcastTimer += delta;
        if (_stateBroadcastTimer >= StateBroadcastSeconds)
        {
            _stateBroadcastTimer = 0.0;
            BroadcastStateDelta();
        }

        if (!_dedicatedServer)
        {
            ApplyStatesToHostAvatars();
        }
    }

    private void UpdateLocalPoseSync()
    {
        if (_localPlayer is null)
        {
            return;
        }

        Vector3 position = _localPlayer.GlobalPosition;
        float yaw = _localPlayer.Rotation.Y;
        float pitch = _localPlayer.ViewPitch;

        if (Multiplayer.IsServer())
        {
            long id = Multiplayer.GetUniqueId();
            _playerStates[id] = new NetPlayerState(position, yaw, pitch);
        }
        else
        {
            RpcId(1, nameof(ServerReceivePose), position, yaw, pitch);
        }
    }

    private void OnPeerConnected(long id)
    {
        if (!Multiplayer.IsServer())
        {
            return;
        }

        if (!_playerStates.ContainsKey(id))
        {
            _playerStates[id] = new NetPlayerState(new Vector3(0f, 42f, 0f), 0f, 0f);
        }

        if (!_playerNames.ContainsKey(id))
        {
            _playerNames[id] = $"Player{id}";
        }

        Rpc(nameof(ClientPlayerNameUpdated), (int)id, _playerNames[id]);
    }

    private void OnPeerDisconnected(long id)
    {
        _playerStates.Remove(id);
        _playerNames.Remove(id);
        RemoveRemoteAvatar(id);

        if (Multiplayer.IsServer())
        {
            Rpc(nameof(ClientPeerLeft), (int)id);
        }
    }

    private void OnConnectedToServer()
    {
        RpcId(1, nameof(ServerClientHello), LaunchOptions.PlayerName);
    }

    private void OnConnectionFailed()
    {
        GD.PushWarning("Connection to server failed.");
    }

    private void OnServerDisconnected()
    {
        GD.PushWarning("Disconnected from server.");
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer)]
    private void ServerClientHello(string playerName)
    {
        if (!Multiplayer.IsServer())
        {
            return;
        }

        long sender = Multiplayer.GetRemoteSenderId();
        string safeName = string.IsNullOrWhiteSpace(playerName) ? $"Player{sender}" : playerName.Trim();
        _playerNames[sender] = safeName;
        if (!_playerStates.ContainsKey(sender))
        {
            _playerStates[sender] = new NetPlayerState(new Vector3(0f, 42f, 0f), 0f, 0f);
        }

        RpcId((int)sender, nameof(ClientJoinAccepted), BuildConfigPayload(), BuildStatePayload());
        Rpc(nameof(ClientPlayerNameUpdated), (int)sender, safeName);
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer)]
    private void ServerReceivePose(Vector3 position, float yaw, float pitch)
    {
        if (!Multiplayer.IsServer())
        {
            return;
        }

        long sender = Multiplayer.GetRemoteSenderId();
        _playerStates[sender] = new NetPlayerState(position, yaw, pitch);
    }

	    [Rpc(MultiplayerApi.RpcMode.Authority)]
	    private void ClientJoinAccepted(Godot.Collections.Dictionary configPayload, Godot.Collections.Array statePayload)
	    {
	        ApplyConfigPayload(configPayload);
	        if (_world is not null && _localPlayer is not null && _world.PlayerTarget is null)
	        {
	            _world.PlayerTarget = _localPlayer;
	        }
	        ApplyStatePayload(statePayload, updateHostLocalAvatars: true);
	    }

    [Rpc(MultiplayerApi.RpcMode.Authority)]
    private void ClientStateDelta(Godot.Collections.Array statePayload)
    {
        ApplyStatePayload(statePayload, updateHostLocalAvatars: false);
    }

    [Rpc(MultiplayerApi.RpcMode.Authority)]
    private void ClientPlayerNameUpdated(int peerId, string displayName)
    {
        long id = peerId;
        _playerNames[id] = string.IsNullOrWhiteSpace(displayName) ? $"Player{id}" : displayName;

        if (_remoteAvatars.TryGetValue(id, out RemotePlayerAvatar? avatar))
        {
            avatar.SetDisplayName(_playerNames[id]);
        }
    }

    [Rpc(MultiplayerApi.RpcMode.Authority)]
    private void ClientPeerLeft(int peerId)
    {
        long id = peerId;
        _playerStates.Remove(id);
        _playerNames.Remove(id);
        RemoveRemoteAvatar(id);
    }

    private void BroadcastStateDelta()
    {
        Godot.Collections.Array payload = BuildStatePayload();
        Rpc(nameof(ClientStateDelta), payload);
    }

    private Godot.Collections.Dictionary BuildConfigPayload()
    {
        if (_world is null)
        {
            return new Godot.Collections.Dictionary();
        }

        World.WorldConfig c = _world.Config;
        Godot.Collections.Dictionary dict = new()
        {
            ["seed"] = c.Seed,
            ["chunk_size"] = c.ChunkSize,
            ["chunk_height"] = c.ChunkHeight,
            ["view_distance"] = c.ViewDistanceChunks,
            ["base_height"] = c.BaseTerrainHeight,
            ["terrain_amp"] = c.TerrainAmplitude,
            ["terrain_freq"] = c.TerrainFrequency,
        };
        return dict;
    }

    private void ApplyConfigPayload(Godot.Collections.Dictionary payload)
    {
        if (_world is null)
        {
            return;
        }

        World.WorldConfig c = _world.Config;
        if (payload.TryGetValue("seed", out Variant seed)) c.Seed = seed.AsInt32();
        if (payload.TryGetValue("chunk_size", out Variant chunkSize)) c.ChunkSize = chunkSize.AsInt32();
        if (payload.TryGetValue("chunk_height", out Variant chunkHeight)) c.ChunkHeight = chunkHeight.AsInt32();
        if (payload.TryGetValue("view_distance", out Variant viewDistance)) c.ViewDistanceChunks = viewDistance.AsInt32();
        if (payload.TryGetValue("base_height", out Variant baseHeight)) c.BaseTerrainHeight = baseHeight.AsInt32();
        if (payload.TryGetValue("terrain_amp", out Variant amp)) c.TerrainAmplitude = amp.AsInt32();
        if (payload.TryGetValue("terrain_freq", out Variant freq)) c.TerrainFrequency = freq.AsSingle();
    }

    private Godot.Collections.Array BuildStatePayload()
    {
        Godot.Collections.Array payload = new();

        foreach ((long id, NetPlayerState state) in _playerStates)
        {
            string name = _playerNames.TryGetValue(id, out string? existingName) ? existingName : $"Player{id}";
            Godot.Collections.Dictionary entry = new()
            {
                ["id"] = (int)id,
                ["name"] = name,
                ["px"] = state.Position.X,
                ["py"] = state.Position.Y,
                ["pz"] = state.Position.Z,
                ["yaw"] = state.Yaw,
                ["pitch"] = state.Pitch,
            };
            payload.Add(entry);
        }

        return payload;
    }

	    private void ApplyStatePayload(Godot.Collections.Array payload, bool updateHostLocalAvatars)
	    {
	        long localId = Multiplayer.GetUniqueId();

	        for (int i = 0; i < payload.Count; i++)
	        {
	            Variant item = payload[i];
	            if (item.VariantType != Variant.Type.Dictionary)
	            {
	                continue;
	            }
	            Godot.Collections.Dictionary entry = item.AsGodotDictionary();

	            long id = entry["id"].AsInt32();
            string name = entry["name"].AsString();
            Vector3 position = new(
                entry["px"].AsSingle(),
                entry["py"].AsSingle(),
                entry["pz"].AsSingle());
            float yaw = entry["yaw"].AsSingle();
            float pitch = entry["pitch"].AsSingle();

            _playerNames[id] = name;
            _playerStates[id] = new NetPlayerState(position, yaw, pitch);

            if (id == localId)
            {
                continue;
            }

            RemotePlayerAvatar avatar = EnsureRemoteAvatar(id);
            avatar.SetDisplayName(name);
            avatar.SetNetworkPose(position, yaw);
        }

        if (Multiplayer.IsServer() && updateHostLocalAvatars)
        {
            ApplyStatesToHostAvatars();
        }
    }

    private void ApplyStatesToHostAvatars()
    {
        long localId = Multiplayer.GetUniqueId();
        foreach ((long id, NetPlayerState state) in _playerStates)
        {
            if (id == localId)
            {
                continue;
            }

            RemotePlayerAvatar avatar = EnsureRemoteAvatar(id);
            if (_playerNames.TryGetValue(id, out string? name))
            {
                avatar.SetDisplayName(name);
            }

            avatar.SetNetworkPose(state.Position, state.Yaw);
        }
    }

    private RemotePlayerAvatar EnsureRemoteAvatar(long peerId)
    {
        if (_remoteAvatars.TryGetValue(peerId, out RemotePlayerAvatar? existing))
        {
            return existing;
        }

        RemotePlayerAvatar avatar = new()
        {
            Name = $"RemotePlayer_{peerId}",
        };
        AddChild(avatar);
        _remoteAvatars[peerId] = avatar;
        return avatar;
    }

    private void RemoveRemoteAvatar(long peerId)
    {
        if (!_remoteAvatars.TryGetValue(peerId, out RemotePlayerAvatar? avatar))
        {
            return;
        }

        _remoteAvatars.Remove(peerId);
        avatar.QueueFree();
    }
}
