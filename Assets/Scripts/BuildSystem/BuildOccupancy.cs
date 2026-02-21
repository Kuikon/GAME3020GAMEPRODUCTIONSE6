using System.Collections.Generic;
using UnityEngine;

public class BuildOccupancy
{
    private readonly Dictionary<Vector3Int, GameObject> occupied = new();

    public bool ValidateBox(GridManager grid, Vector3Int originCell, Vector3Int sizeXYZ, out string reason)
    {
        reason = "";

        foreach (var c in grid.GetCellsInBox(originCell, sizeXYZ))
        {
            if (!grid.IsInside(c)) { reason = $"OUTSIDE {c}"; return false; }
            if (occupied.TryGetValue(c, out var obj) && obj != null) { reason = $"OCCUPIED {c} by {obj.name}"; return false; }
        }

        return true;
    }

    public void RegisterObjectCells(GridManager grid, Vector3Int originCell, Vector3Int sizeXYZ, GameObject obj)
    {
        foreach (var c in grid.GetCellsInBox(originCell, sizeXYZ))
            occupied[c] = obj;
    }

    public bool TryGetObjectAtCell(Vector3Int cell, out GameObject obj)
    {
        return occupied.TryGetValue(cell, out obj);
    }

    public void RemoveObjectCells(GridManager grid, Vector3Int originCell, Vector3Int sizeXYZ)
    {
        foreach (var c in grid.GetCellsInBox(originCell, sizeXYZ))
            occupied.Remove(c);
    }

    public void ClearCell(Vector3Int cell) => occupied.Remove(cell);
}