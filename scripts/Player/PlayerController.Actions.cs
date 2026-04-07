using Godot;
using justonlytnt.World;

namespace justonlytnt.Player;

public sealed partial class PlayerController
{
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
