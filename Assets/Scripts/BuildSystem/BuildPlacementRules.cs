using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// JUDGMENT + STATE(Occupancy merged)
/// - 本来は Judgment は状態を持たないのが理想だが、クラス数を減らすために Occupancy を統合版
/// </summary>
public class BuildPlacementRules
{
    private readonly GridManager grid;

    // ---- Occupancy(State) merged here ----
    private readonly Dictionary<Vector3Int, GameObject> occupied = new();

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
        // いまは制限なし（必要なら1x1Only判定など追加）
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

    // ============================================================
    // Occupancy API (State)
    // ============================================================

    /// <summary>State: セルにオブジェクトを登録</summary>
    public void RegisterObjectCells(Vector3Int originCell, Vector3Int sizeXYZ, GameObject obj)
    {
        if (grid == null) return;

        foreach (var c in grid.GetCellsInBox(originCell, sizeXYZ))
            occupied[c] = obj;
    }

    /// <summary>State: 指定セルにあるオブジェクトを取得</summary>
    public bool TryGetObjectAtCell(Vector3Int cell, out GameObject obj)
    {
        return occupied.TryGetValue(cell, out obj);
    }

    /// <summary>State: box分の登録を削除</summary>
    public void RemoveObjectCells(Vector3Int originCell, Vector3Int sizeXYZ)
    {
        if (grid == null) return;

        foreach (var c in grid.GetCellsInBox(originCell, sizeXYZ))
            occupied.Remove(c);
    }

    /// <summary>State: 1セルだけ削除</summary>
    public void ClearCell(Vector3Int cell) => occupied.Remove(cell);

    /// <summary>State: 念のため全クリア</summary>
    public void ClearAll() => occupied.Clear();
}