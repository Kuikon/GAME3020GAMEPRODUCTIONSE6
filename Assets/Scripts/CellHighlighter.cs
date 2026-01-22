using UnityEngine;

public class CellHighlighter : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private GridManager gridManager;
    [SerializeField] private GridPointer gridPointer; // Åöí«â¡

    [Header("Visual")]
    [SerializeField] private float yOffset = 0.02f;

    private GameObject highlightObj;
    private Vector2Int lastCell;
    private bool hasLast;

    private void Awake()
    {
        highlightObj = GameObject.CreatePrimitive(PrimitiveType.Quad);
        highlightObj.name = "CellHighlight";
        Destroy(highlightObj.GetComponent<Collider>());

        highlightObj.transform.rotation = Quaternion.Euler(90f, 0f, 0f);

        var r = highlightObj.GetComponent<Renderer>();
        r.material = new Material(Shader.Find("Unlit/Color"));
        r.material.color = new Color(1f, 1f, 1f, 0.25f);

        highlightObj.SetActive(false);
    }

    private void OnDestroy()
    {
        if (highlightObj != null) Destroy(highlightObj);
    }

    private void Update()
    {
        if (gridManager == null || gridPointer == null) return;

        if (gridPointer.TryGetCellUnderPointer(out var cell, out _))
        {
            if (!hasLast || cell != lastCell)
            {
                lastCell = cell;
                hasLast = true;

                Vector3 pos = gridManager.CellToWorldCenter(cell);
                pos.y += yOffset;
                highlightObj.transform.position = pos;

                float s = gridManager.CellSize;
                highlightObj.transform.localScale = new Vector3(s, s, 1f);
            }

            if (!highlightObj.activeSelf) highlightObj.SetActive(true);
        }
        else
        {
            if (highlightObj.activeSelf) highlightObj.SetActive(false);
            hasLast = false;
        }
    }
}
