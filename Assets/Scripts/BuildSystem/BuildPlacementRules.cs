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

        if (data == null)
        {
            reason = "DATA_NULL";
            return false;
        }

        // Start / Goal ‚Í1ŒÂ‚¾‚¯‚É‚µ‚½‚¢‚Ì‚Å Line Tool •s‰Â
        if (data.Category == ObjectCategory.Start)
        {
            reason = "START_LINE_NOT_ALLOWED";
            return false;
        }

        if (data.Category == ObjectCategory.Goal)
        {
            reason = "GOAL_LINE_NOT_ALLOWED";
            return false;
        }

        return true;
    }

    // -------------------------
    // Placement judgment (cell occupancy only)
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

    // -------------------------
    // Placement judgment (object-aware)
    // Start / Goal uniqueness + normal occupancy
    // -------------------------
    public bool CanPlaceObject(
        ObjectData data,
        ObjectsDatabaseSO database,
        Vector3Int originCell,
        Vector3Int sizeXYZ,
        out string reason)
    {
        reason = "";

        if (data == null)
        {
            reason = "DATA_NULL";
            return false;
        }

        if (!CanPlaceUniqueCategory(data, database, null, out reason))
            return false;

        return CanPlace(originCell, sizeXYZ, out reason);
    }

    public bool CanPlaceObjectIgnoring(
        BlockInstance ignore,
        ObjectData data,
        ObjectsDatabaseSO database,
        Vector3Int originCell,
        Vector3Int sizeXYZ,
        out string reason)
    {
        reason = "";

        if (data == null)
        {
            reason = "DATA_NULL";
            return false;
        }

        if (!CanPlaceUniqueCategory(data, database, ignore, out reason))
            return false;

        return CanPlaceIgnoring(ignore, originCell, sizeXYZ, out reason);
    }

    // -------------------------
    // Unique category judgment
    // -------------------------
    public bool CanPlaceUniqueCategory(
        ObjectData data,
        ObjectsDatabaseSO database,
        BlockInstance ignore,
        out string reason)
    {
        reason = "";

        if (data == null)
        {
            reason = "DATA_NULL";
            return false;
        }

        // Start ‚Í1ŒÂ‚¾‚¯
        if (data.Category == ObjectCategory.Start)
        {
            if (HasCategoryPlaced(database, ObjectCategory.Start, ignore))
            {
                reason = "START_ALREADY_EXISTS";
                return false;
            }
        }

        // Goal ‚Í1ŒÂ‚¾‚¯
        if (data.Category == ObjectCategory.Goal)
        {
            if (HasCategoryPlaced(database, ObjectCategory.Goal, ignore))
            {
                reason = "GOAL_ALREADY_EXISTS";
                return false;
            }
        }

        return true;
    }

    public bool HasCategoryPlaced(
        ObjectsDatabaseSO database,
        ObjectCategory category,
        BlockInstance ignore = null)
    {
        foreach (var obj in EnumerateUniqueOccupiedObjects())
        {
            if (obj == null) continue;
            if (obj == ignore) continue;

            if (!TryGetObjectCategory(obj, database, out var placedCategory))
                continue;

            if (placedCategory == category)
                return true;
        }

        return false;
    }

    public int CountCategoryPlaced(
        ObjectsDatabaseSO database,
        ObjectCategory category,
        BlockInstance ignore = null)
    {
        int count = 0;

        foreach (var obj in EnumerateUniqueOccupiedObjects())
        {
            if (obj == null) continue;
            if (obj == ignore) continue;

            if (!TryGetObjectCategory(obj, database, out var placedCategory))
                continue;

            if (placedCategory == category)
                count++;
        }

        return count;
    }

    // -------------------------
    // Helpers
    // -------------------------
    private IEnumerable<BlockInstance> EnumerateUniqueOccupiedObjects()
    {
        HashSet<BlockInstance> unique = new();

        foreach (var pair in occupied)
        {
            var obj = pair.Value;
            if (obj == null) continue;

            if (unique.Add(obj))
                yield return obj;
        }
    }

    private bool TryGetObjectCategory(
        BlockInstance obj,
        ObjectsDatabaseSO database,
        out ObjectCategory category)
    {
        category = ObjectCategory.None;

        if (obj == null || database == null)
            return false;

        if (!database.TryGetByID(obj.ObjectID, out ObjectData data) || data == null)
            return false;

        category = data.Category;
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

    public void ClearCell(Vector3Int cell)
    {
        occupied.Remove(cell);
    }

    public void ClearAll()
    {
        occupied.Clear();
    }
}