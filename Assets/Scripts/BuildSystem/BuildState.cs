using UnityEngine;

[System.Serializable]
public sealed class BuildState
{
    public BuildTool PlaceTool = BuildTool.Single;
    public int SelectedObjectID = 0;

    public BlockColor SelectedColor = BlockColor.Blue;

    public bool HasLineStart { get; private set; }
    public Vector3Int LineStartCell { get; private set; }

    public bool HasMoveTarget { get; private set; }
    public BlockInstance MoveTarget { get; private set; }

    [SerializeField] private int rotationStep = 0;

    public Quaternion CurrentRotation
    {
        get { return Quaternion.Euler(0f, rotationStep * 90f, 0f); }
    }

    public void RotateCW()
    {
        rotationStep = (rotationStep + 1) % 4;
    }

    public void RotateCCW()
    {
        rotationStep = (rotationStep + 3) % 4;
    }

    public void SetSelectedObject(int objectID)
    {
        SelectedObjectID = objectID;
    }

    public void SetSelectedColor(BlockColor color)
    {
        SelectedColor = color;
    }

    public void BeginLine(Vector3Int startCell)
    {
        HasLineStart = true;
        LineStartCell = startCell;
    }

    public void CancelLine()
    {
        HasLineStart = false;
        LineStartCell = default;
    }

    public void BeginMove(BlockInstance target)
    {
        HasMoveTarget = target != null;
        MoveTarget = target;
    }

    public void CancelMove()
    {
        HasMoveTarget = false;
        MoveTarget = null;
    }

    public void ResetRotation()
    {
        rotationStep = 0;
    }
}