using UnityEngine;

public class BuildState
{
    public int SelectedObjectID { get; private set; }
    public BuildController.PlaceToolMode PlaceTool { get; private set; }

    // ---------- Line ----------
    public bool HasLineStart { get; private set; }
    public Vector3Int LineStartCell { get; private set; }

    // ---------- Move ----------
    public BlockInstance MoveTarget { get; private set; }
    public bool HasMoveTarget => MoveTarget != null;

    // ---------- Rotation ----------
    // 0=0‹, 1=90‹, 2=180‹, 3=270‹
    public int RotationStep { get; private set; } = 0;
    public Quaternion CurrentRotation => Quaternion.Euler(0f, RotationStep * 90f, 0f);

    public BuildState(int initialSelectedObjectID, BuildController.PlaceToolMode initialTool)
    {
        SelectedObjectID = initialSelectedObjectID;
        PlaceTool = initialTool;
    }

    // -------------------------------------------------------
    // Tool
    // -------------------------------------------------------
    public void ToggleTool()
    {
        PlaceTool = (PlaceTool == BuildController.PlaceToolMode.Single)
            ? BuildController.PlaceToolMode.Line
            : BuildController.PlaceToolMode.Single;
    }

    public void SetTool(BuildController.PlaceToolMode tool)
    {
        PlaceTool = tool;
    }

    // -------------------------------------------------------
    // Selection
    // -------------------------------------------------------
    public void SetSelectedObjectID(int id)
    {
        SelectedObjectID = id;
    }

    public void StepSelection(int delta, int count)
    {
        if (count <= 0) return;

        SelectedObjectID += delta;

        if (SelectedObjectID < 0)
            SelectedObjectID = count - 1;
        else if (SelectedObjectID >= count)
            SelectedObjectID = 0;
    }

    // -------------------------------------------------------
    // Line
    // -------------------------------------------------------
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

    // -------------------------------------------------------
    // Move
    // -------------------------------------------------------
    public void BeginMove(BlockInstance target)
    {
        MoveTarget = target;
    }

    public void CancelMove()
    {
        MoveTarget = null;
    }

    // -------------------------------------------------------
    // Rotation
    // -------------------------------------------------------
    public void RotateCW()
    {
        RotationStep = (RotationStep + 1) % 4;
    }

    public void RotateCCW()
    {
        RotationStep = (RotationStep + 3) % 4;
    }

    public void ResetRotation()
    {
        RotationStep = 0;
    }
}