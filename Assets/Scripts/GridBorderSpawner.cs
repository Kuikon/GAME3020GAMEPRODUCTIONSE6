using UnityEngine;

public class GridBorderSpawner : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private GridManager gridManager;
    [SerializeField] private BuildController buildController;

    [Header("Prefab")]
    [SerializeField] private GameObject borderWallPrefab;

    [Header("Border Settings")]
    [SerializeField] private int borderThickness = 1; // 1=äOé¸1É}ÉX

    [Header("Spawn Height")]
    [SerializeField] private float spawnYOffset = 0.02f; // ÅöPlaneÇÊÇËè≠Çµè„

    private void Start()
    {
        if (gridManager == null || borderWallPrefab == null)
        {
            Debug.LogWarning("[GridBorderSpawner] Missing refs.");
            return;
        }

        SpawnBorder();
    }

    private void SpawnBorder()
    {
        int w = gridManager.Width;
        int h = gridManager.Height;
        int t = Mathf.Max(1, borderThickness);

        for (int x = 0; x < w; x++)
        {
            for (int k = 0; k < t; k++)
            {
                TrySpawn(new Vector2Int(x, 0 + k));
                TrySpawn(new Vector2Int(x, (h - 1) - k));
            }
        }

        for (int y = 0; y < h; y++)
        {
            for (int k = 0; k < t; k++)
            {
                TrySpawn(new Vector2Int(0 + k, y));
                TrySpawn(new Vector2Int((w - 1) - k, y));
            }
        }
    }

    private void TrySpawn(Vector2Int cell)
    {
        if (!gridManager.IsInside(cell)) return;

        if (buildController != null && buildController.IsOccupied(cell))
            return;

        Vector3 pos = gridManager.CellToWorldCenter(cell);
        pos.y += spawnYOffset; // ÅöÇ±Ç±Ç≈è≠Çµè„Ç…Ç∑ÇÈ

        GameObject obj = Instantiate(borderWallPrefab, pos, Quaternion.identity);
        obj.name = $"Border_{cell.x}_{cell.y}";

        if (buildController != null)
            buildController.RegisterPlaced(cell, obj);
    }
}
