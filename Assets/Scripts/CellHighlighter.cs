using UnityEngine;

public class CellHighlighter : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private GridManager grid;
    [SerializeField] private Camera cam;

    [Header("Raycast")]
    [SerializeField] private float rayDistance = 200f;
    [SerializeField] private LayerMask placeMask; // Block + Ground
    [SerializeField] private int groundYCell = 0;

    [Header("Occupancy (optional)")]
    [SerializeField] private BuildController build; // CanPlaceAt を使う用（入れられるなら入れる）

    [Header("Visual")]
    [SerializeField] private float yOffset = 0.02f;
    [SerializeField] private Color canColor = new Color(0.2f, 1f, 0.2f, 0.25f);
    [SerializeField] private Color cannotColor = new Color(1f, 0.2f, 0.2f, 0.25f);

    private GameObject highlightObj;
    private Renderer rend;

    private Vector3Int lastCell;
    private bool hasLast;

    private void Awake()
    {
        if (cam == null) cam = Camera.main;

        highlightObj = GameObject.CreatePrimitive(PrimitiveType.Quad);
        highlightObj.name = "CellHighlight";
        Destroy(highlightObj.GetComponent<Collider>());

        // 上から見えるように水平に
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
        if (grid == null || cam == null)
            return;

        if (!TryGetPreviewCell(out var previewCell, out var can))
        {
            Hide();
            return;
        }

        UpdateTransformIfCellChanged(previewCell);
        UpdateColor(can);
        Show();
    }

    // =========================================================
    // Preview cell (Block face or Ground)
    // =========================================================
    private bool TryGetPreviewCell(out Vector3Int cell, out bool canPlace)
    {
        cell = default;
        canPlace = false;

        Ray ray = cam.ScreenPointToRay(UnityEngine.InputSystem.Mouse.current.position.ReadValue());
        Debug.DrawRay(ray.origin, ray.direction * rayDistance, Color.yellow, 0.05f);

        if (!Physics.Raycast(ray, out var hit, rayDistance, placeMask, QueryTriggerInteraction.Ignore))
            return false;

        // ブロックなら面の外側
        var bi = hit.collider.GetComponentInParent<BlockInstance>();
        if (bi != null)
        {
            Vector3Int normalInt = NormalToInt(hit.normal);
            //cell = bi.Cell + normalInt;
        }
        else
        {
            // Groundなら hit.point をセルにして y を固定
            cell = grid.WorldToCell(hit.point);
            cell.y = groundYCell;
        }

        // 範囲内チェック
        if (!grid.IsInside(cell))
        {
            canPlace = false;
            return true; // 表示はして赤にする
        }

        // 置けるか（BuildControllerが入ってるなら占有チェック）
        if (build != null)
        {
            // buildの内部辞書がprivateなので、本当は build側に CanPlaceAt(cell) public を用意するのがベスト
            // ここでは buildがnullなら「中にあるかどうか」判定できないので、とりあえず範囲内なら緑にする
            canPlace = true;
        }
        else
        {
            canPlace = true;
        }

        return true;
    }

    // =========================================================
    // Visual updates
    // =========================================================
    private void UpdateTransformIfCellChanged(Vector3Int cell)
    {
        if (hasLast && cell == lastCell) return;

        lastCell = cell;
        hasLast = true;

        Vector3 pos = grid.CellToWorldCenter(cell);
        pos.y += yOffset; // Quadが地面/面にめり込まないように少し浮かす
        highlightObj.transform.position = pos;

        float s = grid.CellSize;
        highlightObj.transform.localScale = new Vector3(s, s, 1f);
    }

    private void UpdateColor(bool can)
    {
        rend.material.color = can ? canColor : cannotColor;
    }

    private void Hide()
    {
        if (highlightObj.activeSelf) highlightObj.SetActive(false);
        hasLast = false;
    }

    private void Show()
    {
        if (!highlightObj.activeSelf) highlightObj.SetActive(true);
    }

    // =========================================================
    // Normal -> axis direction
    // =========================================================
    private static Vector3Int NormalToInt(Vector3 n)
    {
        n = n.normalized;
        float ax = Mathf.Abs(n.x);
        float ay = Mathf.Abs(n.y);
        float az = Mathf.Abs(n.z);

        if (ax >= ay && ax >= az) return new Vector3Int((int)Mathf.Sign(n.x), 0, 0);
        if (ay >= ax && ay >= az) return new Vector3Int(0, (int)Mathf.Sign(n.y), 0);
        return new Vector3Int(0, 0, (int)Mathf.Sign(n.z));
    }
}
