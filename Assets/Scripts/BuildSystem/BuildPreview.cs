using System.Collections.Generic;
using UnityEngine;

public sealed class BuildPreview
{
    private readonly GridManager grid;
    private readonly Material previewMaterial;

    private GameObject singlePreview;
    private readonly List<GameObject> linePreviews = new List<GameObject>();

    private GameObject movePreview;
    private GameObject movePreviewSource;

    private ObjectData currentSelectedData;
    private bool lastValid = true;

    public BuildPreview(GridManager grid, Material previewMaterial)
    {
        this.grid = grid;
        this.previewMaterial = previewMaterial;
    }

    public void SetSelected(ObjectData data)
    {
        if (currentSelectedData == data)
            return;

        currentSelectedData = data;
        RebuildPlacementPreviewsOnly();
    }

    public void ShowSingle(Vector3Int cell, Vector3Int size, Quaternion rot)
    {
        if (grid == null || currentSelectedData == null || currentSelectedData.Prefab == null)
            return;

        EnsureSinglePreview();
        HideMovePreview();
        HideLinePreviews();

        if (singlePreview == null)
            return;

        singlePreview.transform.position = grid.BoxToWorldCenter(cell, size);
        singlePreview.transform.rotation = rot;
        singlePreview.SetActive(true);
    }

    public void ShowLine(List<Vector3Int> cells, Vector3Int size, Quaternion rot)
    {
        if (grid == null || currentSelectedData == null || currentSelectedData.Prefab == null)
            return;

        if (cells == null || cells.Count == 0)
        {
            Clear();
            return;
        }

        EnsureLineCount(cells.Count);

        HideSinglePreview();
        HideMovePreview();

        for (int i = 0; i < linePreviews.Count; i++)
        {
            GameObject go = linePreviews[i];
            if (go == null)
                continue;

            bool active = i < cells.Count;
            go.SetActive(active);

            if (!active)
                continue;

            go.transform.position = grid.BoxToWorldCenter(cells[i], size);
            go.transform.rotation = rot;
        }
    }

    public void ShowMovePreview(GameObject targetObject, Vector3 worldPosition)
    {
        if (targetObject == null)
        {
            HideMovePreview();
            return;
        }

        EnsureMovePreview(targetObject);

        HideSinglePreview();
        HideLinePreviews();

        if (movePreview == null)
            return;

        movePreview.transform.position = worldPosition;
        movePreview.transform.rotation = targetObject.transform.rotation;
        movePreview.transform.localScale = targetObject.transform.localScale;
        movePreview.SetActive(true);
    }

    public void SetValid(bool valid)
    {
        if (lastValid == valid)
            return;

        lastValid = valid;

        ApplyColor(singlePreview, valid);
        ApplyColor(movePreview, valid);

        for (int i = 0; i < linePreviews.Count; i++)
            ApplyColor(linePreviews[i], valid);
    }

    public void Clear()
    {
        HideSinglePreview();
        HideLinePreviews();
        HideMovePreview();
    }

    public void ClearActiveOnly()
    {
        Clear();
    }

    private void RebuildPlacementPreviewsOnly()
    {
        DestroyGO(singlePreview);
        singlePreview = null;

        for (int i = 0; i < linePreviews.Count; i++)
            DestroyGO(linePreviews[i]);

        linePreviews.Clear();
    }

    private void EnsureSinglePreview()
    {
        if (singlePreview != null)
            return;

        singlePreview = CreatePreviewFromSelectedPrefab();
    }

    private void EnsureLineCount(int count)
    {
        while (linePreviews.Count < count)
            linePreviews.Add(CreatePreviewFromSelectedPrefab());
    }
    private void ForceRenderersVisible(GameObject go)
    {
        if (go == null)
            return;

        Renderer[] renderers = go.GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] != null)
                renderers[i].enabled = true;
        }
    }
    private void EnsureMovePreview(GameObject targetObject)
    {
        if (movePreview != null && movePreviewSource == targetObject)
            return;

        DestroyGO(movePreview);
        movePreview = null;
        movePreviewSource = targetObject;

        movePreview = Object.Instantiate(targetObject);
        movePreview.name = targetObject.name + "_MovePreview";
        ForceRenderersVisible(movePreview);
        StripComponentsForPreview(movePreview);
        ApplyPreviewMaterial(movePreview);
        ApplyColor(movePreview, lastValid);
        movePreview.SetActive(false);
    }

    private GameObject CreatePreviewFromSelectedPrefab()
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
        if (go == null)
            return;

        Collider[] colliders = go.GetComponentsInChildren<Collider>(true);
        for (int i = 0; i < colliders.Length; i++)
        {
            if (colliders[i] != null)
                Object.Destroy(colliders[i]);
        }

        Rigidbody[] rigidbodies = go.GetComponentsInChildren<Rigidbody>(true);
        for (int i = 0; i < rigidbodies.Length; i++)
        {
            if (rigidbodies[i] != null)
                Object.Destroy(rigidbodies[i]);
        }

        BlockInstance[] blockInstances = go.GetComponentsInChildren<BlockInstance>(true);
        for (int i = 0; i < blockInstances.Length; i++)
        {
            if (blockInstances[i] != null)
                Object.Destroy(blockInstances[i]);
        }

        MonoBehaviour[] behaviours = go.GetComponentsInChildren<MonoBehaviour>(true);
        for (int i = 0; i < behaviours.Length; i++)
        {
            if (behaviours[i] == null)
                continue;

            behaviours[i].enabled = false;
        }
    }

    private void ApplyPreviewMaterial(GameObject go)
    {
        if (go == null || previewMaterial == null)
            return;

        Renderer[] renderers = go.GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer r = renderers[i];
            if (r == null)
                continue;

            Material[] mats = new Material[r.sharedMaterials.Length];
            for (int j = 0; j < mats.Length; j++)
                mats[j] = previewMaterial;

            r.sharedMaterials = mats;
        }
    }

    private void ApplyColor(GameObject go, bool valid)
    {
        if (go == null)
            return;

        Color color = valid
            ? new Color(0f, 1f, 0f, 0.35f)
            : new Color(1f, 0f, 0f, 0.35f);

        Renderer[] renderers = go.GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer r = renderers[i];
            if (r == null)
                continue;

            Material[] mats = r.materials;
            for (int j = 0; j < mats.Length; j++)
            {
                if (mats[j] == null)
                    continue;

                if (mats[j].HasProperty("_Color"))
                    mats[j].color = color;

                if (mats[j].HasProperty("_BaseColor"))
                    mats[j].SetColor("_BaseColor", color);
            }
        }
    }

    private void HideSinglePreview()
    {
        if (singlePreview != null)
            singlePreview.SetActive(false);
    }

    private void HideLinePreviews()
    {
        for (int i = 0; i < linePreviews.Count; i++)
        {
            if (linePreviews[i] != null)
                linePreviews[i].SetActive(false);
        }
    }

    private void HideMovePreview()
    {
        if (movePreview != null)
            movePreview.SetActive(false);
    }

    private void DestroyGO(GameObject go)
    {
        if (go == null)
            return;

        if (Application.isPlaying)
            Object.Destroy(go);
        else
            Object.DestroyImmediate(go);
    }
}