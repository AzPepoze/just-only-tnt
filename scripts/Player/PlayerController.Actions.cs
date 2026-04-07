using Godot;
using justonlytnt.World;

namespace justonlytnt.Player;

public sealed partial class PlayerController
{
    private void BuildFillPreview()
    {
        _fillPreviewMesh = new BoxMesh { Size = Vector3.One };

        StandardMaterial3D previewMaterial = new()
        {
            ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
            Transparency = BaseMaterial3D.TransparencyEnum.Alpha,
            CullMode = BaseMaterial3D.CullModeEnum.Disabled,
            AlbedoColor = new Color(1f, 0.25f, 0.2f, 0.22f),
        };

        _fillPreview = new MeshInstance3D
        {
            Mesh = _fillPreviewMesh,
            MaterialOverride = previewMaterial,
            CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
            TopLevel = true,
            Visible = false,
        };

        AddChild(_fillPreview);
    }

    private bool TryRaycast(float distance, out Vector3 position, out Vector3 normal)
    {
        position = Vector3.Zero;
        normal = Vector3.Up;

        if (_camera is null)
        {
            return false;
        }

        Vector3 origin = _camera.GlobalPosition;
        Vector3 end = origin + (-_camera.GlobalBasis.Z * distance);

        PhysicsRayQueryParameters3D query = PhysicsRayQueryParameters3D.Create(origin, end);
        query.CollideWithAreas = false;
        query.CollideWithBodies = true;
        query.Exclude = new Godot.Collections.Array<Rid> { GetRid() };

        Godot.Collections.Dictionary hit = GetWorld3D().DirectSpaceState.IntersectRay(query);
        if (hit.Count == 0)
        {
            return false;
        }

        position = (Vector3)hit["position"];
        normal = (Vector3)hit["normal"];
        return true;
    }

    private static Vector3I ToBlock(Vector3 pos)
    {
        return new Vector3I(Mathf.FloorToInt(pos.X), Mathf.FloorToInt(pos.Y), Mathf.FloorToInt(pos.Z));
    }

    private void HandleSecondaryActionState(float delta)
    {
        if (_selectedItem != HotbarItem.Tnt)
        {
            bool placePressed = _placePressedQueued;
            _placePressedQueued = false;
            _placeReleasedQueued = false;
            ResetFillPlacementState();

            if (placePressed)
            {
                HandleSecondaryAction();
            }

            return;
        }

        bool placePressedForTnt = _placePressedQueued;
        _placePressedQueued = false;
        if (placePressedForTnt && TryGetTntPlacementBlock(out Vector3I startBlock))
        {
            _fillPlacementStarted = true;
            _fillModeActive = false;
            _fillHoldTimer = 0f;
            _fillStartBlock = startBlock;
            _fillEndBlock = startBlock;
            HideFillPreview();
        }

        if (_fillPlacementStarted && _cachedPlaceHeld)
        {
            _fillHoldTimer += delta;

            if (TryGetTntPlacementBlock(out Vector3I currentBlock))
            {
                _fillEndBlock = currentBlock;
            }

            if (_fillModeActive)
            {
                UpdateFillPreviewVolume();
            }
            else if (_fillHoldTimer >= FillHoldSeconds)
            {
                _fillModeActive = true;
                UpdateFillPreviewVolume();
            }
        }
        else if (_fillPlacementStarted && !_placeReleasedQueued)
        {
            // Input focus changes can occasionally drop release events; avoid sticky placement state.
            ResetFillPlacementState();
        }

        bool placeReleased = _placeReleasedQueued;
        _placeReleasedQueued = false;
        if (!placeReleased || !_fillPlacementStarted || _tnt is null)
        {
            return;
        }

        if (TryGetTntPlacementBlock(out Vector3I releaseBlock))
        {
            _fillEndBlock = releaseBlock;
        }

        if (_fillModeActive)
        {
            _tnt.FillTntBox(_fillStartBlock, _fillEndBlock);
        }
        else
        {
            _tnt.PlaceTntAt(_fillEndBlock);
        }

        ResetFillPlacementState();
    }

    private bool TryGetTntPlacementBlock(out Vector3I placeBlock)
    {
        placeBlock = Vector3I.Zero;

        if (_world is null)
        {
            return false;
        }

        if (!TryRaycast(_world.Config.InteractionDistance, out Vector3 hitPos, out Vector3 normal))
        {
            return false;
        }

        Vector3I targetBlock = ToBlock(hitPos - (normal * 0.01f));
        placeBlock = targetBlock + new Vector3I(
            Mathf.RoundToInt(normal.X),
            Mathf.RoundToInt(normal.Y),
            Mathf.RoundToInt(normal.Z));
        return true;
    }

    private void UpdateFillPreviewVolume()
    {
        if (_fillPreview is null || _fillPreviewMesh is null)
        {
            return;
        }

        Vector3I min = new(
            Mathf.Min(_fillStartBlock.X, _fillEndBlock.X),
            Mathf.Min(_fillStartBlock.Y, _fillEndBlock.Y),
            Mathf.Min(_fillStartBlock.Z, _fillEndBlock.Z));

        Vector3I max = new(
            Mathf.Max(_fillStartBlock.X, _fillEndBlock.X),
            Mathf.Max(_fillStartBlock.Y, _fillEndBlock.Y),
            Mathf.Max(_fillStartBlock.Z, _fillEndBlock.Z));

        _fillPreviewMesh.Size = new Vector3(
            max.X - min.X + 1,
            max.Y - min.Y + 1,
            max.Z - min.Z + 1);

        Vector3 center = new(
            (min.X + max.X + 1) * 0.5f,
            (min.Y + max.Y + 1) * 0.5f,
            (min.Z + max.Z + 1) * 0.5f);

        _fillPreview.GlobalTransform = new Transform3D(Basis.Identity, center);
        _fillPreview.Visible = true;
    }

    private void HideFillPreview()
    {
        if (_fillPreview is not null)
        {
            _fillPreview.Visible = false;
        }
    }

    private void ResetFillPlacementState()
    {
        _fillPlacementStarted = false;
        _fillModeActive = false;
        _fillHoldTimer = 0f;
        HideFillPreview();
    }

    private void HandlePrimaryAction()
    {
        if (_world is null || _tnt is null)
        {
            return;
        }

        if (!TryRaycast(_world.Config.InteractionDistance, out Vector3 hitPos, out Vector3 normal))
        {
            return;
        }

        Vector3I targetBlock = ToBlock(hitPos - (normal * 0.01f));
        BlockType targetedType = _world.GetBlock(targetBlock);

        if (_selectedItem == HotbarItem.FlintAndSteel && targetedType == BlockType.Tnt)
        {
            _tnt.IgniteTnt(targetBlock);
            return;
        }

        if (Multiplayer.MultiplayerPeer is not null && !Multiplayer.IsServer())
        {
            return;
        }

        if (targetedType != BlockType.Air)
        {
            _world.SetBlock(targetBlock, BlockType.Air);
        }
    }

    private void HandleSecondaryAction()
    {
        if (_world is null || _tnt is null)
        {
            return;
        }

        if (!TryRaycast(_world.Config.InteractionDistance, out Vector3 hitPos, out Vector3 normal))
        {
            return;
        }

        Vector3I targetBlock = ToBlock(hitPos - (normal * 0.01f));
        if (_selectedItem == HotbarItem.Tnt)
        {
            _tnt.PlaceTnt(targetBlock, normal);
        }
        else if (_selectedItem == HotbarItem.FlintAndSteel)
        {
            _tnt.IgniteTnt(targetBlock);
        }
    }

    private bool IsStandingChunkReady()
    {
        if (_world is null)
        {
            return false;
        }

        Vector3I feet = ToBlock(GlobalPosition + Vector3.Down * 0.1f);
        ChunkCoord coord = _world.WorldToChunk(feet);
        return _world.IsChunkCollisionReady(coord);
    }
}
