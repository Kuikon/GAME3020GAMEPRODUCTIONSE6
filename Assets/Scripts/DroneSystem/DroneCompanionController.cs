using System.Collections;
using UnityEngine;

public class DroneCompanionController : MonoBehaviour
{
    public enum DroneState
    {
        Idle,
        React,
        Build,
        Remove
    }

    [Header("Laser Pulse")]
    [SerializeField] private float pulseSpeed = 6f;
    [SerializeField] private float pulseMinWidth = 0.03f;
    [SerializeField] private float pulseMaxWidth = 0.15f;

    [Header("Boy Follow Target")]
    [SerializeField] private Transform boyTarget;

    [Header("Follow Offset")]
    [SerializeField] private Vector3 localOffset = new Vector3(0.8f, 1.6f, -0.6f);
    [SerializeField] private float followLerpSpeed = 8f;

    [Header("Float Motion")]
    [SerializeField] private float floatAmplitude = 0.08f;
    [SerializeField] private float floatFrequency = 2.0f;

    [Header("Rotate")]
    [SerializeField] private float rotateSpeed = 12f;

    [Header("React")]
    [SerializeField] private float reactHeightOffset = 1.0f;
    [SerializeField] private float reactMoveLerpSpeed = 10f;

    [Header("Build / Remove Hover")]
    [SerializeField] private Vector3 buildHoverOffset = new Vector3(0f, 1.6f, 0f);
    [SerializeField] private float buildMoveLerpSpeed = 14f;
    [SerializeField] private float buildDuration = 0.8f;

    [Header("Laser Roots")]
    [SerializeField] private Transform laserStartPoint;
    [SerializeField] private Transform[] extraLaserStartPoints;

    [Header("Laser Renderers")]
    [SerializeField] private LineRenderer mainLaser;
    [SerializeField] private LineRenderer[] extraLasers;

    [Header("Laser Visual")]
    [SerializeField] private float laserAppearSpeed = 18f;
    [SerializeField] private float laserMaxAlpha = 1f;

    [Header("Bounds Sampling")]
    [SerializeField] private float boundsTopSurfaceOffset = 0.02f;
    [SerializeField] private bool useRendererBoundsFirst = true;

    private DroneState currentState = DroneState.Idle;

    public bool IsBuilding => currentState == DroneState.Build;
    public bool IsRemoving => currentState == DroneState.Remove;
    public bool IsBusy => currentState == DroneState.Build || currentState == DroneState.Remove;

    private Vector3 reactWorldTarget;
    private bool hasReactTarget;

    private GameObject currentBuildObject;
    private Coroutine buildRoutine;

    private Transform removeTarget;
    private Coroutine removeRoutine;

    private LineRenderer[] cachedLasers;
    private Transform[] cachedLaserStarts;

    private void Awake()
    {
        CacheLaserArrays();
        HideAllLasersImmediate();
    }

    private void Update()
    {
        switch (currentState)
        {
            case DroneState.Idle:
                UpdateIdle();
                break;

            case DroneState.React:
                UpdateReact();
                break;

            case DroneState.Build:
                UpdateBuildFollowOnly();
                break;

            case DroneState.Remove:
                UpdateRemoveFollowOnly();
                break;
        }
    }

    // -------------------------------------------------------
    // Public API
    // -------------------------------------------------------
    public void SetIdle()
    {
        currentState = DroneState.Idle;
        hasReactTarget = false;
        currentBuildObject = null;

        if (buildRoutine != null)
        {
            StopCoroutine(buildRoutine);
            buildRoutine = null;
        }

        if (removeRoutine != null)
        {
            StopCoroutine(removeRoutine);
            removeRoutine = null;
        }

        removeTarget = null;
        HideAllLasersImmediate();
    }

    public void SetReactTarget(Vector3 worldPos)
    {
        reactWorldTarget = worldPos;
        hasReactTarget = true;

        if (currentState != DroneState.Build && currentState != DroneState.Remove)
            currentState = DroneState.React;
    }

    public void PlayBuild(GameObject spawnedObject)
    {
        if (spawnedObject == null)
            return;
        Debug.Log("[Drone] PlayBuild target = " + spawnedObject.name);
        currentBuildObject = spawnedObject;
        currentState = DroneState.Build;

        if (buildRoutine != null)
            StopCoroutine(buildRoutine);

        buildRoutine = StartCoroutine(CoBuildSequence());
    }

    public void PlayRemove(Transform target)
    {
        if (target == null)
        {
            SetIdle();
            return;
        }

        if (removeRoutine != null)
            StopCoroutine(removeRoutine);

        removeRoutine = StartCoroutine(CoRemove(target));
    }

    // -------------------------------------------------------
    // State Updates
    // -------------------------------------------------------
    private void UpdateIdle()
    {
        Vector3 targetPos = GetIdleFollowPosition();
        MoveDrone(targetPos, followLerpSpeed);
        RotateDrone(GetLookTargetPosition());
    }

    private void UpdateReact()
    {
        if (!hasReactTarget)
        {
            UpdateIdle();
            return;
        }

        Vector3 targetPos = reactWorldTarget + Vector3.up * reactHeightOffset;
        targetPos += GetFloatOffset();

        MoveDrone(targetPos, reactMoveLerpSpeed);
        RotateDrone(reactWorldTarget);
    }

    private void UpdateBuildFollowOnly()
    {
        if (currentBuildObject == null)
            return;

        Vector3 targetPos = GetBuildHoverPosition(currentBuildObject);
        MoveDrone(targetPos, buildMoveLerpSpeed);

        if (TryGetObjectBounds(currentBuildObject, out var bounds))
            RotateDrone(bounds.center);
        else
            RotateDrone(currentBuildObject.transform.position);

        UpdateLaserEndpointsToCurrentBuildObject();
    }

    private void UpdateRemoveFollowOnly()
    {
        if (removeTarget == null)
            return;

        if (TryGetObjectBounds(removeTarget.gameObject, out var bounds))
        {
            Vector3 targetPos = bounds.center + buildHoverOffset + GetFloatOffset();
            MoveDrone(targetPos, buildMoveLerpSpeed);
            RotateDrone(bounds.center);
        }
        else
        {
            Vector3 targetPos = removeTarget.position + buildHoverOffset + GetFloatOffset();
            MoveDrone(targetPos, buildMoveLerpSpeed);
            RotateDrone(removeTarget.position);
        }

        UpdateRemoveLaser();
    }

    // -------------------------------------------------------
    // Build / Remove Sequences
    // -------------------------------------------------------
    private IEnumerator CoBuildSequence()
    {
        ShowAllLasersImmediate();

        float time = 0f;

        while (time < buildDuration)
        {
            if (currentBuildObject == null)
                break;

            UpdateBuildFollowOnly();
            FadeLasersToward(laserMaxAlpha);

            time += Time.deltaTime;
            yield return null;
        }

        float fadeTime = 0.12f;
        float t = 0f;

        while (t < fadeTime)
        {
            if (currentBuildObject != null)
                UpdateBuildFollowOnly();

            float alpha = Mathf.Lerp(laserMaxAlpha, 0f, t / fadeTime);
            SetLaserAlpha(alpha);
            FadeLasersToward(alpha);
            PulseLasers();

            t += Time.deltaTime;
            yield return null;
        }

        HideAllLasersImmediate();
        currentBuildObject = null;
        buildRoutine = null;
        currentState = DroneState.Idle;
    }

    private IEnumerator CoRemove(Transform target)
    {
        currentState = DroneState.Remove;
        removeTarget = target;

        ShowAllLasersImmediate();

        while (removeTarget != null)
        {
            UpdateRemoveFollowOnly();
            FadeLasersToward(laserMaxAlpha);
            PulseLasers();

            yield return null;
        }

        HideAllLasersImmediate();
        currentState = DroneState.Idle;
        removeRoutine = null;
    }

    // -------------------------------------------------------
    // Pulse
    // -------------------------------------------------------
    private void PulseLasers()
    {
        if (cachedLasers == null)
            return;

        float t = Mathf.Sin(Time.time * pulseSpeed) * 0.5f + 0.5f;
        float width = Mathf.Lerp(pulseMinWidth, pulseMaxWidth, t);

        for (int i = 0; i < cachedLasers.Length; i++)
        {
            LineRenderer lr = cachedLasers[i];
            if (lr == null)
                continue;

            lr.startWidth = width;
            lr.endWidth = width;
        }
    }

    // -------------------------------------------------------
    // Position Helpers
    // -------------------------------------------------------
    private Vector3 GetIdleFollowPosition()
    {
        if (boyTarget == null)
            return transform.position;

        Vector3 worldOffset = boyTarget.TransformDirection(localOffset);
        Vector3 basePos = boyTarget.position + worldOffset;
        return basePos + GetFloatOffset();
    }

    private Vector3 GetBuildHoverPosition(GameObject targetObject)
    {
        if (targetObject == null)
            return transform.position;

        if (TryGetObjectBounds(targetObject, out var bounds))
        {
            Vector3 basePos = bounds.center + buildHoverOffset;
            return basePos + GetFloatOffset();
        }

        return targetObject.transform.position + buildHoverOffset + GetFloatOffset();
    }

    private Vector3 GetLookTargetPosition()
    {
        if (boyTarget == null)
            return transform.position + transform.forward;

        return boyTarget.position + Vector3.up * 1.0f;
    }

    private Vector3 GetFloatOffset()
    {
        float y = Mathf.Sin(Time.time * floatFrequency) * floatAmplitude;
        return new Vector3(0f, y, 0f);
    }

    private void MoveDrone(Vector3 targetPos, float lerpSpeed)
    {
        transform.position = Vector3.Lerp(
            transform.position,
            targetPos,
            Time.deltaTime * lerpSpeed
        );
    }

    private void RotateDrone(Vector3 lookTarget)
    {
        Vector3 toTarget = lookTarget - transform.position;
        toTarget.y = 0f;

        if (toTarget.sqrMagnitude < 0.0001f)
            return;

        Quaternion targetRot = Quaternion.LookRotation(toTarget.normalized, Vector3.up);
        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            targetRot,
            Time.deltaTime * rotateSpeed
        );
    }

    // -------------------------------------------------------
    // Remove Laser
    // -------------------------------------------------------
    private void UpdateRemoveLaser()
    {
        if (removeTarget == null)
            return;

        if (!TryGetObjectBounds(removeTarget.gameObject, out var bounds))
            return;

        Vector3[] corners = GetTopFourCorners(bounds);

        for (int i = 0; i < cachedLasers.Length; i++)
        {
            LineRenderer lr = cachedLasers[i];
            if (lr == null)
                continue;

            Transform start = GetLaserStartTransform(i);
            if (start == null)
                continue;

            lr.enabled = true;
            lr.positionCount = 2;
            lr.SetPosition(0, start.position);
            lr.SetPosition(1, corners[Mathf.Clamp(i, 0, corners.Length - 1)]);
        }
    }

    // -------------------------------------------------------
    // Build Laser
    // -------------------------------------------------------
    private void UpdateLaserEndpointsToCurrentBuildObject()
    {
        if (currentBuildObject == null)
        {
            HideAllLasersImmediate();
            return;
        }

        if (!TryGetObjectBounds(currentBuildObject, out var bounds))
        {
            HideAllLasersImmediate();
            return;
        }

        Vector3[] corners = GetTopFourCorners(bounds);

        for (int i = 0; i < cachedLasers.Length; i++)
        {
            LineRenderer lr = cachedLasers[i];
            if (lr == null)
                continue;

            Transform start = GetLaserStartTransform(i);
            if (start == null)
                continue;

            lr.enabled = true;
            lr.positionCount = 2;
            lr.SetPosition(0, start.position);
            lr.SetPosition(1, corners[Mathf.Clamp(i, 0, corners.Length - 1)]);
        }
    }

    private Transform GetLaserStartTransform(int index)
    {
        if (cachedLaserStarts == null)
            return null;

        if (index < 0 || index >= cachedLaserStarts.Length)
            return null;

        return cachedLaserStarts[index];
    }

    private Vector3[] GetTopFourCorners(Bounds bounds)
    {
        Vector3 center = bounds.center;
        Vector3 ext = bounds.extents;
        float y = bounds.max.y + boundsTopSurfaceOffset;

        Vector3[] corners = new Vector3[4];
        corners[0] = new Vector3(center.x - ext.x, y, center.z - ext.z);
        corners[1] = new Vector3(center.x - ext.x, y, center.z + ext.z);
        corners[2] = new Vector3(center.x + ext.x, y, center.z - ext.z);
        corners[3] = new Vector3(center.x + ext.x, y, center.z + ext.z);

        return corners;
    }

    // -------------------------------------------------------
    // Bounds
    // -------------------------------------------------------
    private bool TryGetObjectBounds(GameObject targetObject, out Bounds bounds)
    {
        bounds = default;

        if (targetObject == null)
            return false;

        if (useRendererBoundsFirst)
        {
            if (TryGetRendererBounds(targetObject, out bounds))
                return true;

            if (TryGetColliderBounds(targetObject, out bounds))
                return true;
        }
        else
        {
            if (TryGetColliderBounds(targetObject, out bounds))
                return true;

            if (TryGetRendererBounds(targetObject, out bounds))
                return true;
        }

        bounds = new Bounds(targetObject.transform.position, Vector3.one * 0.5f);
        return true;
    }

    private bool TryGetRendererBounds(GameObject targetObject, out Bounds bounds)
    {
        bounds = default;

        Renderer[] renderers = targetObject.GetComponentsInChildren<Renderer>();
        if (renderers == null || renderers.Length == 0)
            return false;

        bool found = false;
        Bounds merged = default;

        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer r = renderers[i];
            if (r == null || !r.enabled)
                continue;

            if (!found)
            {
                merged = r.bounds;
                found = true;
            }
            else
            {
                merged.Encapsulate(r.bounds);
            }
        }

        if (!found)
            return false;

        bounds = merged;
        return true;
    }

    private bool TryGetColliderBounds(GameObject targetObject, out Bounds bounds)
    {
        bounds = default;

        Collider[] colliders = targetObject.GetComponentsInChildren<Collider>();
        if (colliders == null || colliders.Length == 0)
            return false;

        bool found = false;
        Bounds merged = default;

        for (int i = 0; i < colliders.Length; i++)
        {
            Collider c = colliders[i];
            if (c == null || !c.enabled)
                continue;

            if (!found)
            {
                merged = c.bounds;
                found = true;
            }
            else
            {
                merged.Encapsulate(c.bounds);
            }
        }

        if (!found)
            return false;

        bounds = merged;
        return true;
    }

    // -------------------------------------------------------
    // Laser cache
    // -------------------------------------------------------
    private void CacheLaserArrays()
    {
        int extraCount = extraLasers != null ? extraLasers.Length : 0;
        cachedLasers = new LineRenderer[1 + extraCount];
        cachedLasers[0] = mainLaser;

        for (int i = 0; i < extraCount; i++)
            cachedLasers[i + 1] = extraLasers[i];

        int extraStartCount = extraLaserStartPoints != null ? extraLaserStartPoints.Length : 0;
        cachedLaserStarts = new Transform[1 + extraStartCount];
        cachedLaserStarts[0] = laserStartPoint;

        for (int i = 0; i < extraStartCount; i++)
            cachedLaserStarts[i + 1] = extraLaserStartPoints[i];
    }

    // -------------------------------------------------------
    // Laser visual helpers
    // -------------------------------------------------------
    private void ShowAllLasersImmediate()
    {
        if (cachedLasers == null)
            return;

        for (int i = 0; i < cachedLasers.Length; i++)
        {
            if (cachedLasers[i] == null)
                continue;

            cachedLasers[i].enabled = true;
            cachedLasers[i].positionCount = 2;
        }

        SetLaserAlpha(laserMaxAlpha);
    }

    private void HideAllLasersImmediate()
    {
        if (cachedLasers == null)
            return;

        for (int i = 0; i < cachedLasers.Length; i++)
        {
            if (cachedLasers[i] == null)
                continue;

            cachedLasers[i].enabled = false;
            cachedLasers[i].positionCount = 0;
        }
    }

    private void FadeLasersToward(float targetAlpha)
    {
        if (cachedLasers == null)
            return;

        for (int i = 0; i < cachedLasers.Length; i++)
        {
            LineRenderer lr = cachedLasers[i];
            if (lr == null)
                continue;

            Color start = lr.startColor;
            Color end = lr.endColor;

            float nextStartA = Mathf.Lerp(start.a, targetAlpha, Time.deltaTime * laserAppearSpeed);
            float nextEndA = Mathf.Lerp(end.a, targetAlpha, Time.deltaTime * laserAppearSpeed);

            start.a = nextStartA;
            end.a = nextEndA;

            lr.startColor = start;
            lr.endColor = end;
        }
    }

    private void SetLaserAlpha(float alpha)
    {
        if (cachedLasers == null)
            return;

        for (int i = 0; i < cachedLasers.Length; i++)
        {
            LineRenderer lr = cachedLasers[i];
            if (lr == null)
                continue;

            Color start = lr.startColor;
            Color end = lr.endColor;

            start.a = alpha;
            end.a = alpha;

            lr.startColor = start;
            lr.endColor = end;
        }
    }
}