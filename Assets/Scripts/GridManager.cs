// =========================================================
// GridManager.cs
// =========================================================
using System.Collections.Generic;
using UnityEngine;

public class GridManager : MonoBehaviour
{
    [SerializeField] private float cellSize = 1f;
    [SerializeField] private Vector3Int size = new Vector3Int(50, 50, 50);
    [SerializeField] private Transform gridCenter;

    public float CellSize => cellSize;
    public Vector3Int Size => size;

    private Vector3 origin;

    private void Awake()
    {
        EnsureGridCenter();
        CalculateOrigin();
    }

    private void EnsureGridCenter()
    {
        if (gridCenter == null) gridCenter = transform;
    }

    private void CalculateOrigin()
    {
        // XZは中心、Yは底
        Vector3 totalXZ = new Vector3(size.x, 0f, size.z) * cellSize;
        origin = gridCenter.position - totalXZ * 0.5f;
        origin.y = gridCenter.position.y;
    }

    public bool IsInside(Vector3Int cell)
    {
        return cell.x >= 0 && cell.x < size.x &&
               cell.y >= 0 && cell.y < size.y &&
               cell.z >= 0 && cell.z < size.z;
    }

    public Vector3Int WorldToCell(Vector3 world)
    {
        Vector3 local = world - origin;

        int x = Mathf.FloorToInt(local.x / cellSize);
        int y = Mathf.FloorToInt(local.y / cellSize);
        int z = Mathf.FloorToInt(local.z / cellSize);

        return new Vector3Int(x, y, z);
    }

    public Vector3 CellToWorldCenter(Vector3Int cell)
    {
        float x = (cell.x + 0.5f) * cellSize;
        float y = (cell.y + 0.5f) * cellSize;
        float z = (cell.z + 0.5f) * cellSize;
        return origin + new Vector3(x, y, z);
    }

    // -------------------------
    // Occupancy helpers (XYZ)
    // -------------------------
    public IEnumerable<Vector3Int> GetCellsInBox(Vector3Int originCell, Vector3Int sizeXYZ)
    {
        for (int dx = 0; dx < sizeXYZ.x; dx++)
            for (int dy = 0; dy < sizeXYZ.y; dy++)
                for (int dz = 0; dz < sizeXYZ.z; dz++)
                    yield return new Vector3Int(originCell.x + dx, originCell.y + dy, originCell.z + dz);
    }

    // 占有ボックスの中心座標（Transformはここに置く）
    public Vector3 BoxToWorldCenter(Vector3Int originCell, Vector3Int sizeXYZ)
    {
        float cx = (originCell.x + sizeXYZ.x * 0.5f) * cellSize;
        float cy = (originCell.y + sizeXYZ.y * 0.5f) * cellSize;
        float cz = (originCell.z + sizeXYZ.z * 0.5f) * cellSize;
        return origin + new Vector3(cx, cy, cz);
    }
}
