using System.Collections.Generic;
using UnityEngine;

public class PrefabThumbnailGenerator : MonoBehaviour
{
    [Header("Capture")]
    [SerializeField] private Camera captureCamera;
    [SerializeField] private int thumbnailWidth = 256;
    [SerializeField] private int thumbnailHeight = 256;
    [SerializeField] private LayerMask previewLayer = 0;
    [SerializeField] private Color backgroundColor = new Color(0f, 0f, 0f, 0f);
    [SerializeField] private bool useTransparentBackground = true;

    [Header("Spawn Root")]
    [SerializeField] private Transform previewRoot;

    [Header("View")]
    [SerializeField] private Vector3 viewDirection = new Vector3(1f, 0.8f, -1f);
    [SerializeField] private float padding = 1.25f;

    [Header("Light")]
    [SerializeField] private Light previewLight;
    [SerializeField] private bool autoCreateLightIfMissing = true;

    private readonly Dictionary<int, Texture2D> cache = new Dictionary<int, Texture2D>();

    public Texture2D GetThumbnail(ObjectData data, BlockColor color)
    {
        if (data == null)
            return null;

        GameObject prefab = data.GetPrefab(color);
        if (prefab == null)
            return null;

        int cacheKey = data.ID * 100 + (int)color;

        if (cache.TryGetValue(cacheKey, out var cached) && cached != null)
            return cached;

        Texture2D tex = CaptureThumbnail(prefab);
        cache[cacheKey] = tex;
        return tex;
    }

    public void Warmup(ObjectsDatabaseSO database)
    {
        if (database == null) return;
        IReadOnlyList<ObjectData> list = database.ObjectsData;
        for (int i = 0; i < list.Count; i++)
        {
            ObjectData data = list[i];
            if (data == null || data.Prefab == null) continue;

            if (!cache.ContainsKey(data.ID))
                cache[data.ID] = CaptureThumbnail(data.Prefab);
        }
    }

    private Texture2D CaptureThumbnail(GameObject prefab)
    {
        if (captureCamera == null || prefab == null)
            return null;

        EnsurePreviewRoot();
        EnsurePreviewLight();

        GameObject instance = Instantiate(prefab, previewRoot);
        instance.transform.localPosition = Vector3.zero;
        instance.transform.localRotation = Quaternion.identity;
        instance.transform.localScale = Vector3.one;

        SetLayerRecursively(instance, LayerMaskToLayerIndex(previewLayer));

        Bounds bounds = CalculateBounds(instance);
        if (bounds.size == Vector3.zero)
        {
            DestroyImmediate(instance);
            return CreateFallbackTexture();
        }

        SetupCamera(bounds);

        RenderTexture rt = new RenderTexture(thumbnailWidth, thumbnailHeight, 24, RenderTextureFormat.ARGB32);
        rt.Create();

        RenderTexture prevActive = RenderTexture.active;
        RenderTexture prevCameraTarget = captureCamera.targetTexture;

        captureCamera.targetTexture = rt;
        captureCamera.clearFlags = CameraClearFlags.SolidColor;
        captureCamera.backgroundColor = backgroundColor;

        if (useTransparentBackground)
            captureCamera.backgroundColor = new Color(0f, 0f, 0f, 0f);

        captureCamera.Render();

        RenderTexture.active = rt;
        Texture2D tex = new Texture2D(thumbnailWidth, thumbnailHeight, TextureFormat.RGBA32, false);
        tex.ReadPixels(new Rect(0, 0, thumbnailWidth, thumbnailHeight), 0, 0);
        tex.Apply();

        captureCamera.targetTexture = prevCameraTarget;
        RenderTexture.active = prevActive;

        rt.Release();
        DestroyImmediate(rt);
        DestroyImmediate(instance);

        return tex;
    }

    private void SetupCamera(Bounds bounds)
    {
        Vector3 dir = viewDirection.normalized;
        Vector3 center = bounds.center;

        float radius = bounds.extents.magnitude;
        if (radius < 0.01f) radius = 0.5f;

        if (captureCamera.orthographic)
        {
            captureCamera.transform.position = center - dir * 10f;
            captureCamera.transform.LookAt(center);
            captureCamera.orthographicSize = radius * padding;
        }
        else
        {
            float fov = captureCamera.fieldOfView * Mathf.Deg2Rad;
            float distance = (radius * padding) / Mathf.Sin(fov * 0.5f);

            captureCamera.transform.position = center - dir * distance;
            captureCamera.transform.LookAt(center);
        }

        if (previewLight != null)
        {
            previewLight.transform.position = center - dir * 2f + Vector3.up * 2f;
            previewLight.transform.LookAt(center);
        }
    }

    private Bounds CalculateBounds(GameObject root)
    {
        Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);

        if (renderers.Length == 0)
            return new Bounds(root.transform.position, Vector3.zero);

        Bounds bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
            bounds.Encapsulate(renderers[i].bounds);

        return bounds;
    }

    private void SetLayerRecursively(GameObject obj, int layer)
    {
        if (obj == null || layer < 0) return;

        obj.layer = layer;
        foreach (Transform t in obj.GetComponentsInChildren<Transform>(true))
            t.gameObject.layer = layer;
    }

    private int LayerMaskToLayerIndex(LayerMask mask)
    {
        int value = mask.value;
        if (value == 0) return 0;

        for (int i = 0; i < 32; i++)
        {
            if ((value & (1 << i)) != 0)
                return i;
        }

        return 0;
    }

    private void EnsurePreviewRoot()
    {
        if (previewRoot != null) return;

        GameObject go = new GameObject("ThumbnailPreviewRoot");
        go.hideFlags = HideFlags.HideAndDontSave;
        previewRoot = go.transform;
    }

    private void EnsurePreviewLight()
    {
        if (previewLight != null) return;
        if (!autoCreateLightIfMissing) return;

        GameObject lightGO = new GameObject("ThumbnailLight");
        lightGO.hideFlags = HideFlags.HideAndDontSave;
        lightGO.transform.SetParent(transform);

        previewLight = lightGO.AddComponent<Light>();
        previewLight.type = LightType.Directional;
        previewLight.intensity = 1.2f;
    }

    private Texture2D CreateFallbackTexture()
    {
        Texture2D tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        tex.SetPixels(new Color[]
        {
            Color.gray, Color.gray,
            Color.gray, Color.gray
        });
        tex.Apply();
        return tex;
    }
}