using UnityEngine;

public class GridManager : MonoBehaviour
{
    [SerializeField] private float cellSize = 1f;
    [SerializeField] private float gridY = 0f;
    [SerializeField] private int width = 20;
    [SerializeField] private int height = 20;
    [SerializeField] private Transform gridCenter;
    public float CellSize => cellSize;
    public float GridY => gridY;
    public int Width => width;
    public int Height => height;
    private Vector3 origin;
    private void Awake()
    {
        if (gridCenter == null)
            gridCenter = transform;

        CalculateOrigin();
    }
    private void CalculateOrigin()
    {
        float totalWidth = width * cellSize;
        float totalHeight = height * cellSize;

        // Plane’†S‚©‚ç”¼•ª‚¸‚ç‚µ‚Ä¶‰º‚ðì‚é
        origin = gridCenter.position
               - new Vector3(totalWidth * 0.5f, 0f, totalHeight * 0.5f);
    }
    public Vector2Int WorldToCell(Vector3 world)
    {
        Vector3 local = world - origin;

        int x = Mathf.FloorToInt(local.x / cellSize);
        int y = Mathf.FloorToInt(local.z / cellSize);

        return new Vector2Int(x, y);
    }

    public Vector3 CellToWorldCenter(Vector2Int cell)
    {
        float x = (cell.x + 0.5f) * cellSize;
        float z = (cell.y + 0.5f) * cellSize;

        return origin + new Vector3(x, 0f, z);
    }
    public bool IsInside(Vector2Int cell)
    {
        return cell.x >= 0 && cell.x < width &&
               cell.y >= 0 && cell.y < height;
    }
}
