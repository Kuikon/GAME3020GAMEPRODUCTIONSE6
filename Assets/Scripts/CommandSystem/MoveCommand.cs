using UnityEngine;

public sealed class MoveCommand : IBuildCommand
{
    private readonly BuildContext context;
    private readonly BlockInstance target;
    private readonly Vector3Int toCell;
    private readonly string commandName;

    private Vector3Int fromCell;
    private Quaternion rotation;
    private Vector3Int size;

    public string Name => commandName;

    public BlockInstance TargetBlock => target;
    public Vector3Int FromCell => fromCell;
    public Vector3Int ToCell => toCell;
    public Quaternion Rotation => rotation;
    public Vector3Int SizeXYZ => size;

    public MoveCommand(
        BuildContext context,
        BlockInstance target,
        Vector3Int toCell,
        string commandName = "Move Block")
    {
        this.context = context;
        this.target = target;
        this.toCell = toCell;
        this.commandName = commandName;

        if (target != null)
        {
            fromCell = target.OriginCell;
            rotation = target.Rotation;
            size = target.SizeXYZ;
        }
    }

    public bool Do(bool debugLogs = false, bool playEffects = true)
    {
        if (context == null || target == null)
            return false;

        if (!context.Rules.CanPlaceIgnoring(target, toCell, size, out string reason))
        {
            if (debugLogs)
                Debug.Log($"[MoveCommand] CanPlaceIgnoring failed -> {toCell} reason={reason}");
            return false;
        }

        ApplyMove(target, toCell);

        if (debugLogs)
            Debug.Log($"[MoveCommand] Success: {fromCell} -> {toCell}");

        return true;
    }

    public void Undo(bool debugLogs = false, bool playEffects = true)
    {
        if (context == null || target == null)
            return;

        ApplyMove(target, fromCell);

        if (debugLogs)
            Debug.Log($"[MoveCommand] Undo: {toCell} -> {fromCell}");
    }

    private void ApplyMove(BlockInstance block, Vector3Int destination)
    {
        if (block == null)
            return;

        context.Rules.RemoveObjectCells(block.OriginCell, block.SizeXYZ);

        Vector3 world = context.Grid.BoxToWorldCenter(destination, size);
        block.transform.position = world;
        block.transform.rotation = rotation;
        block.SetOriginCell(destination);
        block.SetRotation(rotation);

        context.Rules.RegisterObjectCells(destination, size, block);
    }
}