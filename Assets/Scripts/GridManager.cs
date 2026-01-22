using UnityEngine;

public class GridManager : MonoBehaviour
{
    [SerializeField] private float cellSize = 1f;
    [SerializeField] private float gridY = 0f;

    public float CellSize => cellSize;
    public float GridY => gridY;

    public Vector2Int WorldToCell(Vector3 worldPos)
    {
        int x = Mathf.FloorToInt(worldPos.x / cellSize);
        int y = Mathf.FloorToInt(worldPos.z / cellSize);
        return new Vector2Int(x, y);
    }

    public Vector3 CellToWorldCenter(Vector2Int cell)
    {
        float x = (cell.x + 0.5f) * cellSize;
        float z = (cell.y + 0.5f) * cellSize;
        return new Vector3(x, gridY, z);
    }
}
