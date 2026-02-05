using UnityEngine;

public class CellHighlighter : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private GridManager gridManager;
    [SerializeField] private GridPointer gridPointer;
    [SerializeField] private BuildController buildController;

    [Header("Visual")]
    [SerializeField] private float yOffset = 0.02f;
    [SerializeField] private Color canColor = new Color(0.2f, 1f, 0.2f, 0.25f);
    [SerializeField] private Color cannotColor = new Color(1f, 0.2f, 0.2f, 0.25f);

    private GameObject highlightObj;
    private Renderer rend;

    private Vector2Int lastCell;
    private bool hasLast;

    private void Awake()
    {
        highlightObj = GameObject.CreatePrimitive(PrimitiveType.Quad);
        highlightObj.name = "CellHighlight";
        Destroy(highlightObj.GetComponent<Collider>());

        highlightObj.transform.rotation = Quaternion.Euler(90f, 0f, 0f);

        rend = highlightObj.GetComponent<Renderer>();
        rend.material = new Material(Shader.Find("Unlit/Color"));
        rend.material.color = canColor;

        highlightObj.SetActive(false);
    }

    private void OnDestroy()
    {
        if (highlightObj != null) Destroy(highlightObj);
    }

    private void Update()
    {
        if (gridManager == null || gridPointer == null || buildController == null) return;

        if (!gridPointer.TryGetCellUnderPointer(out var cell2, out _))
        {
            if (highlightObj.activeSelf) highlightObj.SetActive(false);
            hasLast = false;
            return;
        }

        // 次に置く3Dセル
        Vector3Int nextCell3 = buildController.GetNextPlaceCellFromFloor(cell2);

        // 位置更新（セル変化時だけ）
        if (!hasLast || cell2 != lastCell)
        {
            lastCell = cell2;
            hasLast = true;

            Vector3 pos = gridManager.CellToWorldCenter(cell2);

            pos.y = buildController.BaseYOffset + (nextCell3.y * buildController.BlockHeight) + yOffset;

            highlightObj.transform.position = pos;

            float s = gridManager.CellSize;
            highlightObj.transform.localScale = new Vector3(s, s, 1f);
        }

        // 色（3Dセルで判定）
        bool isInside = gridManager.IsInside(cell2);
        bool canPlace = buildController.CanPlaceAt3D(nextCell3);
        bool can = isInside && canPlace;
        rend.material.color = can ? canColor : cannotColor;

        if (!highlightObj.activeSelf) highlightObj.SetActive(true);
    }
}
