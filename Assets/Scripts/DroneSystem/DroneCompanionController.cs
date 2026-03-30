using System;
using System.Collections;
using UnityEngine;

public class DroneCompanionController : MonoBehaviour
{
    public enum DroneState
    {
        Idle,
        React,
        Build,
        Remove,
        Move
    }

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

    [Header("Build / Remove / Move Hover")]
    [SerializeField] private Vector3 buildHoverOffset = new Vector3(0f, 1.6f, 0f);
    [SerializeField] private float buildMoveLerpSpeed = 14f;
    [SerializeField] private float removeMoveLerpSpeed = 10f;
    [SerializeField] private float carryMoveLerpSpeed = 12f;
    [SerializeField] private float buildDuration = 0.8f;
    [SerializeField] private float carryCommitFadeDuration = 0.12f;

    [Header("Arrival")]
    [SerializeField] private float arrivalDistance = 0.08f;
    [SerializeField] private float maxApproachTime = 1.5f;

    [Header("Laser Roots")]
    [SerializeField] private Transform laserStartPoint;
    [SerializeField] private Transform[] extraLaserStartPoints;

    [Header("Laser Renderers")]
    [SerializeField] private LineRenderer mainLaser;
    [SerializeField] private LineRenderer[] extraLasers;

    [Header("Laser Visual")]
    [SerializeField] private float laserAppearSpeed = 18f;
    [SerializeField] private float laserMaxAlpha = 1f;

    [Header("Laser Pulse")]
    [SerializeField] private float pulseSpeed = 6f;
    [SerializeField] private float pulseMinWidth = 0.03f;
    [SerializeField] private float pulseMaxWidth = 0.15f;

    [Header("Bounds Sampling")]
    [SerializeField] private float boundsTopSurfaceOffset = 0.02f;
    [SerializeField] private bool useRendererBoundsFirst = true;

    private DroneState currentState = DroneState.Idle;

    private Vector3 reactWorldTarget;
    private bool hasReactTarget;

    private GameObject currentBuildObject;
    private Transform removeTarget;
    private Transform moveTarget;

    private Coroutine buildRoutine;
    private Coroutine removeRoutine;
    private Coroutine carryRoutine;

    private bool laserVisible;

    private LineRenderer[] cachedLasers;
    private Transform[] cachedLaserStarts;

    public event Action SequenceFinished;

    public bool IsBusy => currentState == DroneState.Build || currentState == DroneState.Remove;
    public bool IsCarrying => currentState == DroneState.Move;

    private void Awake()
    {
        CacheLaserArrays();
        laserVisible = false;
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

            case DroneState.Move:
                UpdateMoveFollowOnly();
                break;
        }
    }

    public void SetIdle()
    {
        currentState = DroneState.Idle;
        hasReactTarget = false;
        currentBuildObject = null;
        removeTarget = null;
        moveTarget = null;

        StopBuildRoutine();
        StopRemoveRoutine();
        StopCarryRoutine();

        laserVisible = false;
        HideAllLasersImmediate();
    }

    public void SetReactTarget(Vector3 worldPos)
    {
        reactWorldTarget = worldPos;
        hasReactTarget = true;

        if (currentState != DroneState.Build &&
            currentState != DroneState.Remove &&
            currentState != DroneState.Move)
        {
            currentState = DroneState.React;
        }
    }

    public void PlayBuild(GameObject spawnedObject)
    {
        if (spawnedObject == null)
            return;

        if (IsBusy || IsCarrying)
            return;

        currentBuildObject = spawnedObject;
        currentState = DroneState.Build;

        StopBuildRoutine();
        StopRemoveRoutine();
        StopCarryRoutine();

        laserVisible = false;
        HideAllLasersImmediate();
        SetRenderersVisible(currentBuildObject, false);
        buildRoutine = StartCoroutine(CoBuildSequence());
    }

    public void PlayBuildAt(Vector3 worldPos)
    {
        if (IsBusy || IsCarrying)
            return;

        StopBuildRoutine();
        StopRemoveRoutine();
        StopCarryRoutine();

        buildRoutine = StartCoroutine(CoBuildAtSequence(worldPos));
    }

    public void PlayRemove(Transform target)
    {
        if (target == null)
        {
            SetIdle();
            return;
        }

        if (IsCarrying)
            CancelCarry();

        removeTarget = target;
        currentState = DroneState.Remove;

        StopBuildRoutine();
        StopRemoveRoutine();
        StopCarryRoutine();

        laserVisible = false;
        HideAllLasersImmediate();

        removeRoutine = StartCoroutine(CoRemoveSequence());
    }

    public void BeginCarry(Transform target)
    {
        if (target == null)
        {
            CancelCarry();
            return;
        }

        if (IsBusy)
            return;

        StopBuildRoutine();
        StopRemoveRoutine();
        StopCarryRoutine();

        moveTarget = target;
        hasReactTarget = false;
        currentState = DroneState.Move;

        laserVisible = true;
        ShowAllLasersImmediate();
        SetLaserAlpha(0f);
    }

    public void CommitCarry(Transform target)
    {
        if (target != null)
            moveTarget = target;

        if (moveTarget == null)
        {
            CancelCarry();
            SequenceFinished?.Invoke();
            return;
        }

        StopCarryRoutine();
        carryRoutine = StartCoroutine(CoCommitCarrySequence());
    }

    public void CancelCarry()
    {
        StopCarryRoutine();

        moveTarget = null;
        laserVisible = false;
        HideAllLasersImmediate();

        currentState = DroneState.Idle;
    }

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

        MoveToObject(currentBuildObject, buildMoveLerpSpeed);

        if (laserVisible)
            UpdateLaserToObject(currentBuildObject);
    }

    private void UpdateRemoveFollowOnly()
    {
        if (removeTarget == null)
            return;

        MoveToObject(removeTarget.gameObject, removeMoveLerpSpeed);

        if (laserVisible)
            UpdateLaserToObject(removeTarget.gameObject);
    }

    private void UpdateMoveFollowOnly()
    {
        if (moveTarget == null)
        {
            CancelCarry();
            return;
        }

        MoveToObject(moveTarget.gameObject, carryMoveLerpSpeed);

        if (!laserVisible)
            return;

        ShowAllLasersImmediate();
        FadeLasersToward(laserMaxAlpha);
        PulseLasers();
        UpdateLaserToObject(moveTarget.gameObject);
    }

    private IEnumerator CoBuildSequence()
    {
        yield return CoMoveUntilArrived(currentBuildObject, buildMoveLerpSpeed);

        if (currentBuildObject == null)
        {
            laserVisible = false;
            buildRoutine = null;
            currentState = DroneState.Idle;
            SequenceFinished?.Invoke();
            yield break;
        }

        SetRenderersVisible(currentBuildObject, true);
        PlayBuildEffect(currentBuildObject);

        laserVisible = true;
        ShowAllLasersImmediate();

        float time = 0f;

        while (time < buildDuration)
        {
            if (currentBuildObject == null)
                break;

            UpdateBuildFollowOnly();
            FadeLasersToward(laserMaxAlpha);
            PulseLasers();

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

        laserVisible = false;
        HideAllLasersImmediate();
        currentBuildObject = null;
        buildRoutine = null;
        currentState = DroneState.Idle;

        SequenceFinished?.Invoke();
    }

    private IEnumerator CoBuildAtSequence(Vector3 worldPos)
    {
        currentState = DroneState.Build;
        laserVisible = false;
        HideAllLasersImmediate();

        float time = 0f;

        while (time < maxApproachTime)
        {
            Vector3 targetPos = worldPos + buildHoverOffset + GetFloatOffset();

            MoveDrone(targetPos, buildMoveLerpSpeed);
            RotateDrone(worldPos);

            float distance = Vector3.Distance(transform.position, targetPos);
            if (distance <= arrivalDistance)
                break;

            time += Time.deltaTime;
            yield return null;
        }

        currentState = DroneState.Idle;
        buildRoutine = null;

        SequenceFinished?.Invoke();
    }

    private IEnumerator CoRemoveSequence()
    {
        yield return CoMoveUntilArrived(removeTarget != null ? removeTarget.gameObject : null, removeMoveLerpSpeed);

        if (removeTarget == null)
        {
            laserVisible = false;
            HideAllLasersImmediate();
            currentState = DroneState.Idle;
            removeRoutine = null;
            SequenceFinished?.Invoke();
            yield break;
        }

        laserVisible = true;
        ShowAllLasersImmediate();

        // ‚±‚±‚ÅíœŠJŽn‚³‚¹‚é
        SequenceFinished?.Invoke();

        float removeLaserDuration = 0.5f;
        float t = 0f;

        while (t < removeLaserDuration)
        {
            if (removeTarget == null)
                break;

            UpdateRemoveFollowOnly();
            FadeLasersToward(laserMaxAlpha);
            PulseLasers();

            t += Time.deltaTime;
            yield return null;
        }

        laserVisible = false;
        HideAllLasersImmediate();
        currentState = DroneState.Idle;
        removeRoutine = null;
    }

    private IEnumerator CoCommitCarrySequence()
    {
        currentState = DroneState.Move;
        laserVisible = true;
        ShowAllLasersImmediate();

        float t = 0f;
        while (t < carryCommitFadeDuration)
        {
            if (moveTarget == null)
                break;

            UpdateMoveFollowOnly();

            float alpha = Mathf.Lerp(laserMaxAlpha, 0f, t / carryCommitFadeDuration);
            SetLaserAlpha(alpha);
            FadeLasersToward(alpha);
            PulseLasers();

            t += Time.deltaTime;
            yield return null;
        }

        laserVisible = false;
        HideAllLasersImmediate();
        moveTarget = null;
        carryRoutine = null;
        currentState = DroneState.Idle;

        SequenceFinished?.Invoke();
    }

    private IEnumerator CoMoveUntilArrived(GameObject targetObject, float moveSpeed)
    {
        float time = 0f;

        while (time < maxApproachTime)
        {
            if (targetObject == null)
                yield break;

            Vector3 targetPos = GetBuildHoverPosition(targetObject);
            MoveDrone(targetPos, moveSpeed);

            if (TryGetObjectBounds(targetObject, out Bounds bounds))
                RotateDrone(bounds.center);
            else
                RotateDrone(targetObject.transform.position);

            float distance = Vector3.Distance(transform.position, targetPos);
            if (distance <= arrivalDistance)
                yield break;

            time += Time.deltaTime;
            yield return null;
        }
    }

    private void MoveToObject(GameObject targetObject, float moveSpeed)
    {
        if (targetObject == null)
            return;

        Vector3 targetPos = GetBuildHoverPosition(targetObject);
        MoveDrone(targetPos, moveSpeed);

        if (TryGetObjectBounds(targetObject, out Bounds bounds))
            RotateDrone(bounds.center);
        else
            RotateDrone(targetObject.transform.position);
    }

    private void PlayBuildEffect(GameObject targetObject)
    {
        if (targetObject == null)
            return;

        BuildEffectUtility.PlayBuildEffect(targetObject);
    }

    private void UpdateLaserToObject(GameObject targetObject)
    {
        if (targetObject == null)
        {
            HideAllLasersImmediate();
            return;
        }

        if (!TryGetObjectBounds(targetObject, out Bounds bounds))
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

        if (TryGetObjectBounds(targetObject, out Bounds bounds))
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

    private void MoveDrone(Vector3 targetPos, float moveSpeed)
    {
        transform.position = Vector3.MoveTowards(
            transform.position,
            targetPos,
            moveSpeed * Time.deltaTime);
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
            Time.deltaTime * rotateSpeed);
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

        Renderer[] renderers = targetObject.GetComponentsInChildren<Renderer>(true);
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

        Collider[] colliders = targetObject.GetComponentsInChildren<Collider>(true);
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

    private Transform GetLaserStartTransform(int index)
    {
        if (cachedLaserStarts == null)
            return null;

        if (index < 0 || index >= cachedLaserStarts.Length)
            return null;

        return cachedLaserStarts[index];
    }

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

    private void SetRenderersVisible(GameObject go, bool visible)
    {
        if (go == null)
            return;

        Renderer[] renderers = go.GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] != null)
                renderers[i].enabled = visible;
        }
    }

    private void StopBuildRoutine()
    {
        if (buildRoutine == null)
            return;

        StopCoroutine(buildRoutine);
        buildRoutine = null;
    }

    private void StopRemoveRoutine()
    {
        if (removeRoutine == null)
            return;

        StopCoroutine(removeRoutine);
        removeRoutine = null;
    }

    private void StopCarryRoutine()
    {
        if (carryRoutine == null)
            return;

        StopCoroutine(carryRoutine);
        carryRoutine = null;
    }
}