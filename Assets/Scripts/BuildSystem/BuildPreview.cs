using System.Collections.Generic;
using UnityEngine;

public class BuildPreview
{
    private readonly GridManager grid;
    private readonly Material previewMaterial;

    private GameObject singlePreview;
    private GameObject movePreview;
    private readonly List<GameObject> linePreviews = new List<GameObject>();

    private ObjectData currentSelectedData;
    private bool lastValid = true;

    public BuildPreview(GridManager grid, Material previewMaterial)
    {
        this.grid = grid;
        this.previewMaterial = previewMaterial;
    }

    public void SetSelected(ObjectData data)
    {
        if (currentSelectedData == data) return;

        currentSelectedData = data;
        RebuildAll();
    }

    public void ShowSingle(Vector3Int cell, Vector3Int size, Quaternion rot)
    {
        if (grid == null || currentSelectedData == null || currentSelectedData.Prefab == null)
            return;

        EnsureSinglePreview();

        if (singlePreview == null) return;

        if (movePreview != null)
            movePreview.SetActive(false);

        singlePreview.transform.position = grid.BoxToWorldCenter(cell, size);
        singlePreview.transform.rotation = rot;
        singlePreview.SetActive(true);

        for (int i = 0; i < linePreviews.Count; i++)
        {
            if (linePreviews[i] != null)
                linePreviews[i].SetActive(false);
        }
    }

    public void ShowLine(List<Vector3Int> cells, Vector3Int size, Quaternion rot)
    {
        if (grid == null || currentSelectedData == null || currentSelectedData.Prefab == null)
            return;

        EnsureLineCount(cells.Count);

        if (singlePreview != null)
            singlePreview.SetActive(false);
        if (movePreview != null)
            movePreview.SetActive(false);
        for (int i = 0; i < linePreviews.Count; i++)
        {
            if (linePreviews[i] == null) continue;

            bool active = i < cells.Count;
            linePreviews[i].SetActive(active);

            if (!active) continue;

            linePreviews[i].transform.position = grid.BoxToWorldCenter(cells[i], size);
            linePreviews[i].transform.rotation = rot;
        }
    }

    public void SetValid(bool valid)
    {
        if (lastValid == valid) return;
        lastValid = valid;

        ApplyColor(singlePreview, valid);
        ApplyColor(movePreview, valid);

        for (int i = 0; i < linePreviews.Count; i++)
            ApplyColor(linePreviews[i], valid);
    }

    public void Clear()
    {
        if (singlePreview != null)
            singlePreview.SetActive(false);

        if (movePreview != null)
            movePreview.SetActive(false);

        for (int i = 0; i < linePreviews.Count; i++)
        {
            if (linePreviews[i] != null)
                linePreviews[i].SetActive(false);
        }
    }

    public void ClearActiveOnly()
    {
        Clear();
    }

    // -------------------------------------------------------
    private void RebuildAll()
    {
        DestroyGO(singlePreview);
        singlePreview = null;

        for (int i = 0; i < linePreviews.Count; i++)
            DestroyGO(linePreviews[i]);

        linePreviews.Clear();
    }

    private void EnsureSinglePreview()
    {
        if (singlePreview != null) return;
        singlePreview = CreatePreviewObject();
    }

    private void EnsureLineCount(int count)
    {
        while (linePreviews.Count < count)
        {
            linePreviews.Add(CreatePreviewObject());
        }
    }

    private GameObject CreatePreviewObject()
    {
        if (currentSelectedData == null || currentSelectedData.Prefab == null)
            return null;

        GameObject go = Object.Instantiate(currentSelectedData.Prefab);
        go.name = currentSelectedData.Prefab.name + "_Preview";

        StripComponentsForPreview(go);
        ApplyPreviewMaterial(go);
        ApplyColor(go, lastValid);

        go.SetActive(false);
        return go;
    }

    private void StripComponentsForPreview(GameObject go)
    {
        if (go == null) return;

        var colliders = go.GetComponentsInChildren<Collider>(true);
        foreach (var c in colliders)
            Object.Destroy(c);

        var rigidbodies = go.GetComponentsInChildren<Rigidbody>(true);
        foreach (var rb in rigidbodies)
            Object.Destroy(rb);

        var blockInstances = go.GetComponentsInChildren<BlockInstance>(true);
        foreach (var bi in blockInstances)
            Object.Destroy(bi);

        var behaviours = go.GetComponentsInChildren<MonoBehaviour>(true);
        foreach (var b in behaviours)
        {
            if (b == null) continue;
            if (b is Transform) continue;
            b.enabled = false;
        }
    }

    private void ApplyPreviewMaterial(GameObject go)
    {
        if (go == null || previewMaterial == null) return;

        var renderers = go.GetComponentsInChildren<Renderer>(true);
        foreach (var r in renderers)
        {
            if (r == null) continue;

            var mats = new Material[r.sharedMaterials.Length];
            for (int i = 0; i < mats.Length; i++)
                mats[i] = previewMaterial;

            r.sharedMaterials = mats;
        }
    }

    private void ApplyColor(GameObject go, bool valid)
    {
        if (go == null) return;

        Color color = valid
            ? new Color(0f, 1f, 0f, 0.35f)
            : new Color(1f, 0f, 0f, 0.35f);

        var renderers = go.GetComponentsInChildren<Renderer>(true);
        foreach (var r in renderers)
        {
            if (r == null) continue;

            var mats = r.materials;
            for (int i = 0; i < mats.Length; i++)
            {
                if (mats[i] == null) continue;

                if (mats[i].HasProperty("_Color"))
                    mats[i].color = color;

                if (mats[i].HasProperty("_BaseColor"))
                    mats[i].SetColor("_BaseColor", color);
            }
        }
    }
    public void ShowMovePreview(GameObject targetObject, Vector3 worldPosition)
    {
        if (targetObject == null)
            return;

        if (movePreview == null)
        {
            movePreview = Object.Instantiate(targetObject);
            movePreview.name = targetObject.name + "_MovePreview";

            StripComponentsForPreview(movePreview);
            ApplyPreviewMaterial(movePreview);
            ApplyColor(movePreview, lastValid);
        }

        if (singlePreview != null)
            singlePreview.SetActive(false);

        for (int i = 0; i < linePreviews.Count; i++)
        {
            if (linePreviews[i] != null)
                linePreviews[i].SetActive(false);
        }

        movePreview.transform.position = worldPosition;
        movePreview.transform.rotation = targetObject.transform.rotation;
        movePreview.transform.localScale = targetObject.transform.localScale;
        movePreview.SetActive(true);
    }
    private void DestroyGO(GameObject go)
    {
        if (go == null) return;

        if (Application.isPlaying)
            Object.Destroy(go);
        else
            Object.DestroyImmediate(go);
    }
}