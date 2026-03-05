using System.Collections.Generic;
using UnityEngine;

public class BuildPlacementRules
{
    private readonly GridManager grid;

    // ---- Occupancy(State) merged here ----
    private readonly Dictionary<Vector3Int, BlockInstance> occupied = new();

    public BuildPlacementRules(GridManager grid)
    {
        this.grid = grid;
    }

    // -------------------------
    // Line tool judgment
    // -------------------------
    public bool CanUseLineTool(ObjectData data, out string reason)
    {
        reason = "";

        return true;
    }

    // -------------------------
    // Placement judgment
    // -------------------------
    public bool CanPlace(Vector3Int originCell, Vector3Int sizeXYZ, out string reason)
    {
        reason = "";

        if (grid == null)
        {
            reason = "GRID_NULL";
            return false;
        }

        foreach (var c in grid.GetCellsInBox(originCell, sizeXYZ))
        {
            if (!grid.IsInside(c))
            {
                reason = $"OUTSIDE {c}";
                return false;
            }

            if (occupied.TryGetValue(c, out var obj) && obj != null)
            {
                reason = $"OCCUPIED {c} by {obj.name}";
                return false;
            }
        }

        return true;
    }
    public bool CanPlaceIgnoring(BlockInstance ignore, Vector3Int originCell, Vector3Int sizeXYZ, out string reason)
    {
        reason = "";

        if (grid == null)
        {
            reason = "GRID_NULL";
            return false;
        }

        foreach (var c in grid.GetCellsInBox(originCell, sizeXYZ))
        {
            if (!grid.IsInside(c))
            {
                reason = $"OUTSIDE {c}";
                return false;
            }

            if (occupied.TryGetValue(c, out var bi) && bi != null && bi != ignore)
            {
                reason = $"OCCUPIED {c} by {bi.name}";
                return false;
            }
        }

        return true;
    }
    // ============================================================
    // Occupancy API (State)
    // ============================================================

    public void RegisterObjectCells(Vector3Int originCell, Vector3Int sizeXYZ, BlockInstance obj)
    {
        if (grid == null) return;

        foreach (var c in grid.GetCellsInBox(originCell, sizeXYZ))
            occupied[c] = obj;
    }

    public bool TryGetObjectAtCell(Vector3Int cell, out BlockInstance obj)
    {
        return occupied.TryGetValue(cell, out obj);
    }

    public void RemoveObjectCells(Vector3Int originCell, Vector3Int sizeXYZ)
    {
        if (grid == null) return;

        foreach (var c in grid.GetCellsInBox(originCell, sizeXYZ))
            occupied.Remove(c);
    }
    public void ClearCell(Vector3Int cell) => occupied.Remove(cell);

    public void ClearAll() => occupied.Clear();
}