using Godot;

namespace justonlytnt.Networking;

public enum LaunchMode
{
    Single = 0,
    Host = 1,
    Client = 2,
    DedicatedServer = 3,
}

public static class LaunchOptions
{
    public const int DefaultPort = 24567;
    public const int MaxPlayers = 16;

    public static LaunchMode Mode { get; private set; } = LaunchMode.Single;
    public static string JoinAddress { get; private set; } = "127.0.0.1";
    public static int Port { get; private set; } = DefaultPort;
    public static string PlayerName { get; private set; } = "Player";
    public static int? OverrideSeed { get; private set; }

    private static bool _parsedCli;

    public static void SetSingle(string playerName, int? seed = null)
    {
        Mode = LaunchMode.Single;
        PlayerName = string.IsNullOrWhiteSpace(playerName) ? "Player" : playerName;
        OverrideSeed = seed;
    }

    public static void SetHost(int port, string playerName, int? seed)
    {
        Mode = LaunchMode.Host;
        Port = Mathf.Clamp(port, 1, 65535);
        PlayerName = string.IsNullOrWhiteSpace(playerName) ? "Host" : playerName;
        OverrideSeed = seed;
    }

    public static void SetClient(string address, int port, string playerName)
    {
        Mode = LaunchMode.Client;
        JoinAddress = string.IsNullOrWhiteSpace(address) ? "127.0.0.1" : address.Trim();
        Port = Mathf.Clamp(port, 1, 65535);
        PlayerName = string.IsNullOrWhiteSpace(playerName) ? "Client" : playerName;
    }

    public static void SetDedicatedServer(int port, int? seed)
    {
        Mode = LaunchMode.DedicatedServer;
        Port = Mathf.Clamp(port, 1, 65535);
        PlayerName = "Server";
        OverrideSeed = seed;
    }

    public static void ParseCliOnce()
    {
        if (_parsedCli)
        {
            return;
        }

        _parsedCli = true;
        string[] args = OS.GetCmdlineArgs();
        if (args.Length == 0)
        {
            return;
        }

        bool serverFlag = false;
        bool hostFlag = false;
        bool clientFlag = false;
        string? ip = null;
        int port = DefaultPort;
        int? seed = null;
        string? name = null;

        for (int i = 0; i < args.Length; i++)
        {
            string arg = args[i];
            if (arg == "--server")
            {
                serverFlag = true;
            }
            else if (arg == "--host")
            {
                hostFlag = true;
            }
            else if (arg == "--client")
            {
                clientFlag = true;
            }
            else if ((arg == "--ip" || arg == "--address") && i + 1 < args.Length)
            {
                ip = args[++i];
            }
            else if (arg == "--port" && i + 1 < args.Length)
            {
                if (int.TryParse(args[++i], out int parsedPort))
                {
                    port = parsedPort;
                }
            }
            else if (arg == "--seed" && i + 1 < args.Length)
            {
                if (int.TryParse(args[++i], out int parsedSeed))
                {
                    seed = parsedSeed;
                }
            }
            else if (arg == "--name" && i + 1 < args.Length)
            {
                name = args[++i];
            }
        }

        if (serverFlag)
        {
            SetDedicatedServer(port, seed);
            return;
        }

        if (hostFlag)
        {
            SetHost(port, name ?? "Host", seed);
            return;
        }

        if (clientFlag)
        {
            SetClient(ip ?? "127.0.0.1", port, name ?? "Client");
        }
    }
}
