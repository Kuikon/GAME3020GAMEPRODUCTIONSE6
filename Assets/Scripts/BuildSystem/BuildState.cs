using UnityEngine;

public class BuildState
{
    public BuildController.PlaceToolMode PlaceTool { get; private set; } = BuildController.PlaceToolMode.Single;

    public int SelectedObjectID { get; private set; }

    // Line tool state
    public bool HasLineStart { get; private set; }
    public Vector3Int LineStartCell { get; private set; }

    public BuildState(int initialSelectedId, BuildController.PlaceToolMode initialTool)
    {
        SelectedObjectID = initialSelectedId;
        PlaceTool = initialTool;
    }

    // -------------------------
    // Tool
    // -------------------------
    public void SetTool(BuildController.PlaceToolMode tool)
    {
        PlaceTool = tool;
        CancelLine();
    }

    public void ToggleTool()
    {
        PlaceTool = (PlaceTool == BuildController.PlaceToolMode.Single)
            ? BuildController.PlaceToolMode.Line
            : BuildController.PlaceToolMode.Single;

        CancelLine();
    }

    // -------------------------
    // Selection
    // -------------------------
    public void SetSelectedObjectID(int id)
    {
        SelectedObjectID = id;
        CancelLine();
    }

    public void StepSelection(int delta, int count)
    {
        if (count <= 0) return;

        int id = SelectedObjectID + delta;

        if (id < 0) id = count - 1;
        else if (id >= count) id = 0;

        SelectedObjectID = id;
        CancelLine();
    }

    // -------------------------
    // Line
    // -------------------------
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
}