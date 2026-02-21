using System.Collections.Generic;
using UnityEngine;

public class BuildPreview
{
    private readonly GridManager grid;
    private readonly Material previewMaterial;

    private GameObject previewObj;
    private Renderer[] renderers;
    private int currentID = int.MinValue;
    private static readonly int ColorID = Shader.PropertyToID("_Color");
    private static readonly int BaseColorID = Shader.PropertyToID("_BaseColor");
    // Rendererごとのマテリアルスロット数
    private readonly Dictionary<Renderer, int> materialSlotCount = new();

    public BuildPreview(GridManager grid, Material previewMaterial)
    {
        this.grid = grid;
        this.previewMaterial = previewMaterial;
    }

    public void SetSelected(ObjectData data)
    {
        if (data == null || data.Prefab == null)
        {
            Clear();
            return;
        }

        if (previewObj != null && data.ID == currentID) return;

        currentID = data.ID;

        if (previewObj != null)
            Object.Destroy(previewObj);

        previewObj = Object.Instantiate(data.Prefab);
        previewObj.name = $"PREVIEW_{data.Name}_ID{data.ID}";

        // Preview レイヤーに固定
        ForceLayerRecursive(previewObj, "Preview");

        // 物理無効化（Raycast・衝突防止）
        foreach (var col in previewObj.GetComponentsInChildren<Collider>(true))
            col.enabled = false;

        foreach (var rb in previewObj.GetComponentsInChildren<Rigidbody>(true))
            rb.isKinematic = true;

        // Animatorだけ停止（全部のMonoBehaviour停止は危険）
        foreach (var anim in previewObj.GetComponentsInChildren<Animator>(true))
            anim.enabled = false;

        renderers = previewObj.GetComponentsInChildren<Renderer>(true);

        // Previewマテリアルを全スロットに挿入
        materialSlotCount.Clear();
        if (previewMaterial != null)
        {
            foreach (var r in renderers)
            {
                if (r == null) continue;

                int slots = (r.sharedMaterials != null && r.sharedMaterials.Length > 0)
                    ? r.sharedMaterials.Length
                    : 1;

                materialSlotCount[r] = slots;

                var mats = new Material[slots];
                for (int i = 0; i < slots; i++)
                    mats[i] = previewMaterial;

                r.sharedMaterials = mats;
            }
        }
    }

    public void UpdatePose(Vector3Int originCell, Vector3Int sizeXYZ)
    {
        if (previewObj == null || grid == null) return;

        Vector3 center = grid.BoxToWorldCenter(originCell, sizeXYZ);
        previewObj.transform.position = center;
        previewObj.transform.rotation = Quaternion.identity;
    }

    // 🔹 機能は残すが、何もしない（呼ばれても安全）
    public void SetValid(bool canPlace)
    {
        if (renderers == null) return;

        // 置ける：緑 / 置けない：赤（透明度は同じ）
        Color c = canPlace
            ? new Color(1, 1f, 1f, 0.1f)   // OK = green
            : new Color(1f, 0.2f, 0.2f, 0.1f);  // NG = red

        var mpb = new MaterialPropertyBlock();
        mpb.SetColor(ColorID, c);       // Built-in Standard
        mpb.SetColor(BaseColorID, c);   // URP Lit

        foreach (var r in renderers)
        {
            if (r == null) continue;
            r.SetPropertyBlock(mpb);
        }
    }

    public void Clear()
    {
        currentID = int.MinValue;

        if (previewObj != null)
            Object.Destroy(previewObj);

        previewObj = null;
        renderers = null;
        materialSlotCount.Clear();
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