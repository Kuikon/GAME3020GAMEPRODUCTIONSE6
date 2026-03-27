using UnityEngine;

public class BuildPreviewService
{
    private readonly BuildRaycaster raycaster;
    private readonly BuildPlacementSolver solver;
    private readonly BuildPlacementRules rules;
    private readonly BuildPreview preview;

    public BuildPreviewService(
        BuildRaycaster raycaster,
        BuildPlacementSolver solver,
        BuildPlacementRules rules,
        BuildPreview preview)
    {
        this.raycaster = raycaster;
        this.solver = solver;
        this.rules = rules;
        this.preview = preview;
    }

    public void Clear()
    {
        preview?.Clear();
    }

    public void UpdatePreview(BuildState state, ObjectData data)
    {
        if (preview == null || state == null || data == null)
        {
            preview?.Clear();
            return;
        }

        Quaternion rot = state.CurrentRotation;
        Vector3Int rotatedSize = solver.GetRotatedSize(data.SizeXYZ, rot);

        if (!solver.TryGetHoverCell(raycaster, rotatedSize, out var hoverCell, out _))
        {
            preview.Clear();
            return;
        }

        preview.SetSelected(data);

        if (state.PlaceTool == BuildTool.Single)
        {
            ShowSinglePreview(hoverCell, rotatedSize, rot);
            return;
        }

        ShowLinePreview(state, hoverCell, data, rotatedSize, rot);
    }

    private void ShowSinglePreview(Vector3Int cell, Vector3Int size, Quaternion rot)
    {
        preview.ShowSingle(cell, size, rot);

        bool canPlace = rules.CanPlace(cell, size, out _);
        preview.SetValid(canPlace);
    }

    private void ShowLinePreview(
        BuildState state,
        Vector3Int hoverCell,
        ObjectData data,
        Vector3Int rotatedSize,
        Quaternion rot)
    {
        if (!rules.CanUseLineTool(data, out _))
        {
            preview.ShowSingle(hoverCell, rotatedSize, rot);
            preview.SetValid(false);
            return;
        }

        if (!state.HasLineStart)
        {
            ShowSinglePreview(hoverCell, rotatedSize, rot);
            return;
        }

        if (!solver.TryGetLineCellsOrthogonal(state.LineStartCell, hoverCell, rotatedSize, out var lineCells) ||
            lineCells == null || lineCells.Count == 0)
        {
            preview.ClearActiveOnly();
            return;
        }

        preview.ShowLine(lineCells, rotatedSize, rot);

        bool allPlaceable = true;
        foreach (var c in lineCells)
        {
            if (!rules.CanPlace(c, rotatedSize, out _))
            {
                allPlaceable = false;
                break;
            }
        }

        preview.SetValid(allPlaceable);
    }

}