using System.Collections.Generic;
using UnityEngine;

public class BuildPreview
{
    private readonly GridManager grid;
    private readonly Material previewMaterial;

    // ★単体 → 複数へ（プール）
    private readonly List<GameObject> previewPool = new();

    private int currentID = int.MinValue;

    private static readonly int ColorID = Shader.PropertyToID("_Color");
    private static readonly int BaseColorID = Shader.PropertyToID("_BaseColor");

    // pool全体のRendererに色を当てたいので、毎回集める
    private Renderer[] cachedRenderers;

    public BuildPreview(GridManager grid, Material previewMaterial)
    {
        this.grid = grid;
        this.previewMaterial = previewMaterial;
    }

    // -------------------------
    // Selection
    // -------------------------
    public void SetSelected(ObjectData data)
    {
        if (data == null || data.Prefab == null)
        {
            Clear();
            return;
        }

        // 同じIDで、すでにpreviewがあるなら作り直さない
        if (data.ID == currentID && previewPool.Count > 0) return;

        currentID = data.ID;

        // 選択が変わったら pool を作り直す（初心者向けに安全なやり方）
        DestroyAll();

        // 最低1個は作る（Single用）
        var first = CreatePreviewInstance(data);
        previewPool.Add(first);

        RebuildRendererCache();
    }

    // -------------------------
    // Show Single / Line
    // -------------------------
    public void ShowSingle(Vector3Int originCell, Vector3Int sizeXYZ)
    {
        if (previewPool.Count == 0) return;

        EnsurePoolSize(1);

        // 0番だけ表示
        for (int i = 0; i < previewPool.Count; i++)
            previewPool[i].SetActive(i == 0);

        ApplyPose(previewPool[0], originCell, sizeXYZ);
        RebuildRendererCache();
    }

    public void ShowLine(List<Vector3Int> lineCells, Vector3Int sizeXYZ)
    {
        if (lineCells == null || lineCells.Count == 0)
        {
            ClearActiveOnly();
            return;
        }

        EnsurePoolSize(lineCells.Count);

        for (int i = 0; i < previewPool.Count; i++)
        {
            bool active = i < lineCells.Count;
            previewPool[i].SetActive(active);

            if (active)
                ApplyPose(previewPool[i], lineCells[i], sizeXYZ);
        }

        RebuildRendererCache();
    }

    private void ApplyPose(GameObject obj, Vector3Int originCell, Vector3Int sizeXYZ)
    {
        if (obj == null || grid == null) return;

        Vector3 center = grid.BoxToWorldCenter(originCell, sizeXYZ);
        obj.transform.position = center;
        obj.transform.rotation = Quaternion.identity;
    }

    // -------------------------
    // Valid Color
    // -------------------------
    public void SetValid(bool canPlace)
    {
        if (cachedRenderers == null) return;

        Color c = canPlace
            ? new Color(1f, 1f, 1f, 0.1f)
            : new Color(1f, 0.2f, 0.2f, 0.1f);

        var mpb = new MaterialPropertyBlock();
        mpb.SetColor(ColorID, c);
        mpb.SetColor(BaseColorID, c);

        foreach (var r in cachedRenderers)
        {
            if (r == null) continue;
            r.SetPropertyBlock(mpb);
        }
    }

    // -------------------------
    // Clear
    // -------------------------
    public void Clear()
    {
        currentID = int.MinValue;
        DestroyAll();
        cachedRenderers = null;
    }

    // ★選択は維持したまま “表示だけ消す”
    public void ClearActiveOnly()
    {
        for (int i = 0; i < previewPool.Count; i++)
        {
            if (previewPool[i] != null)
                previewPool[i].SetActive(false);
        }
    }

    private void DestroyAll()
    {
        for (int i = 0; i < previewPool.Count; i++)
        {
            if (previewPool[i] != null)
                Object.Destroy(previewPool[i]);
        }
        previewPool.Clear();
    }

    // -------------------------
    // Pool helpers
    // -------------------------
    private void EnsurePoolSize(int needed)
    {
        if (previewPool.Count == 0) return;

        // 0番を複製して増やす
        while (previewPool.Count < needed)
        {
            var clone = Object.Instantiate(previewPool[0]);
            clone.name = previewPool[0].name + "_Clone";
            previewPool.Add(clone);
        }
    }

    private void RebuildRendererCache()
    {
        var list = new List<Renderer>();

        foreach (var obj in previewPool)
        {
            if (obj == null) continue;
            list.AddRange(obj.GetComponentsInChildren<Renderer>(true));
        }

        cachedRenderers = list.ToArray();
    }

    // -------------------------
    // Create preview instance (あなたのロジックをそのまま利用)
    // -------------------------
    private GameObject CreatePreviewInstance(ObjectData data)
    {
        var previewObj = Object.Instantiate(data.Prefab);
        previewObj.name = $"PREVIEW_{data.Name}_ID{data.ID}";

        ForceLayerRecursive(previewObj, "Preview");

        foreach (var col in previewObj.GetComponentsInChildren<Collider>(true))
            col.enabled = false;

        foreach (var rb in previewObj.GetComponentsInChildren<Rigidbody>(true))
            rb.isKinematic = true;

        foreach (var anim in previewObj.GetComponentsInChildren<Animator>(true))
            anim.enabled = false;

        // マテリアル差し替え（全Rendererスロットに previewMaterial）
        if (previewMaterial != null)
        {
            var renderers = previewObj.GetComponentsInChildren<Renderer>(true);
            foreach (var r in renderers)
            {
                if (r == null) continue;

                int slots = (r.sharedMaterials != null && r.sharedMaterials.Length > 0)
                    ? r.sharedMaterials.Length
                    : 1;

                var mats = new Material[slots];
                for (int i = 0; i < slots; i++)
                    mats[i] = previewMaterial;

                r.sharedMaterials = mats;
            }
        }

        return previewObj;
    }

    private static void ForceLayerRecursive(GameObject obj, string layerName)
    {
        int layer = LayerMask.NameToLayer(layerName);
        if (layer < 0) return;

        obj.layer = layer;
        foreach (Transform t in obj.GetComponentsInChildren<Transform>(true))
            t.gameObject.layer = layer;
    }
}