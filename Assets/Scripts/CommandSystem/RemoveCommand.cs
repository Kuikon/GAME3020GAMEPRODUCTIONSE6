using UnityEngine;

public sealed class RemoveCommand : IBuildCommand
{
    private readonly BuildContext context;
    private readonly Vector3Int anyCell;

    private Vector3Int removedOriginCell;
    private Vector3Int removedSize;
    private Quaternion removedRotation;
    private ObjectData removedData;
    private BlockColor removedColor;

    private BlockInstance removedBlock;
    private GameObject removedObject;

    public string Name => $"Remove @ {anyCell}";

    // BuildApplicationService から参照しやすいように公開
    public Vector3Int RemovedOriginCell => removedOriginCell;
    public Vector3Int RemovedSize => removedSize;
    public Quaternion RemovedRotation => removedRotation;
    public ObjectData RemovedData => removedData;
    public BlockColor RemovedColor => removedColor;
    public GameObject RemovedObject => removedObject;
    public BlockInstance CurrentBlock => removedBlock;

    public RemoveCommand(BuildContext context, Vector3Int anyCell)
    {
        this.context = context;
        this.anyCell = anyCell;
    }

    public bool Do(bool debugLogs = false, bool playEffects = true)
    {
        if (context == null)
            return false;

        if (!context.Rules.TryGetObjectAtCell(anyCell, out removedBlock) || removedBlock == null)
        {
            if (debugLogs)
                Debug.Log($"[RemoveCommand] No block found at {anyCell}");
            return false;
        }

        removedObject = removedBlock.gameObject;
        removedOriginCell = removedBlock.OriginCell;
        removedSize = removedBlock.SizeXYZ;
        removedRotation = removedBlock.Rotation;
        removedColor = removedBlock.Color;

        if (!context.Database.TryGetByID(removedBlock.ObjectID, out removedData) || removedData == null)
        {
            if (debugLogs)
                Debug.Log("[RemoveCommand] Failed to get ObjectData by ID.");
            return false;
        }

        context.Rules.RemoveObjectCells(removedOriginCell, removedSize);

        GameObject target = removedObject;

        if (playEffects)
        {
            context.Drone?.PlayRemove(target != null ? target.transform : null);

            BuildEffectUtility.PlayDestroyEffect(target, () =>
            {
                if (target != null)
                    Object.Destroy(target);
            });
        }
        else
        {
            if (target != null)
                Object.Destroy(target);
        }

        if (debugLogs)
            Debug.Log($"[RemoveCommand] Removed {removedData.Name} ({removedColor}) @ {removedOriginCell}");

        return true;
    }
    public void Undo(bool debugLogs = false, bool playEffects = true)
    {
        if (context == null || removedData == null)
            return;

        GameObject respawned = context.Spawner.Spawn(
            context.Grid,
            removedOriginCell,
            removedData,
            removedRotation,
            removedColor);

        if (respawned == null)
        {
            if (debugLogs)
                Debug.Log("[RemoveCommand] Undo respawn failed.");
            return;
        }

        BlockInstance block = respawned.GetComponent<BlockInstance>();
        if (block == null)
            block = respawned.AddComponent<BlockInstance>();

        block.Initialize(
            removedData.ID,
            removedOriginCell,
            removedSize,
            removedRotation,
            removedColor);

        context.Rules.RegisterObjectCells(removedOriginCell, removedSize, block);

        removedObject = respawned;
        removedBlock = block;

        if (playEffects)
            context.Drone?.PlayBuild(removedObject);

        if (debugLogs)
            Debug.Log($"[RemoveCommand] Undo success @ {removedOriginCell} color={removedColor}");
    }
}