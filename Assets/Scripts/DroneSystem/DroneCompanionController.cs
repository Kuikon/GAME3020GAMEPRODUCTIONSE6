using System;
using System.Collections;
using System.Collections.Generic;
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
    private readonly List<GameObject> currentBuildGroup = new List<GameObject>();
    private Transform removeTarget;
    private Transform moveTarget;

    private Bounds currentBuildBounds;
    private bool hasBuildBounds;

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
        ResetLasersImmediate();
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
                UpdateBuild();
                break;

            case DroneState.Remove:
                UpdateRemove();
                break;

            case DroneState.Move:
                UpdateMove();
                break;
        }
    }

    // ---------------------------------------------------------------------
    // Public API
    // ---------------------------------------------------------------------

    public void SetIdle()
    {
        ClearTargets();
        StopAllSequences();
        EnterIdleState();
    }

    public void SetReactTarget(Vector3 worldPos)
    {
        reactWorldTarget = worldPos;
        hasReactTarget = true;

        if (!IsBusy && !IsCarrying)
            currentState = DroneState.React;
    }

    public void PlayBuild(GameObject spawnedObject)
    {
        if (spawnedObject == null || IsBusy || IsCarrying)
            return;

        PrepareForBuildObject(spawnedObject);
        buildRoutine = StartCoroutine(CoBuildSequence());
    }

    public void PlayBuildGroup(List<GameObject> targets)
    {
        if (targets == null || targets.Count == 0 || IsBusy || IsCarrying)
            return;

        if (!TryGetMergedBounds(targets, out Bounds merged))
            return;

        StopAllSequences();

        currentBuildGroup.Clear();
        for (int i = 0; i < targets.Count; i++)
        {
            GameObject go = targets[i];
            if (go == null)
                continue;

            currentBuildGroup.Add(go);
            SetRenderersVisible(go, false);
        }

        currentBuildBounds = merged;
        hasBuildBounds = true;
        currentBuildObject = null;
        removeTarget = null;
        moveTarget = null;
        currentState = DroneState.Build;

        ResetLasersImmediate();
        buildRoutine = StartCoroutine(CoBuildGroupSequence());
    }

    public void PlayBuildAt(Vector3 worldPos)
    {
        if (IsBusy || IsCarrying)
            return;

        StopAllSequences();
        hasBuildBounds = false;
        currentBuildObject = null;
        currentState = DroneState.Build;
        ResetLasersImmediate();

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

        StopAllSequences();

        hasBuildBounds = false;
        currentBuildObject = null;
        removeTarget = target;
        moveTarget = null;
        currentState = DroneState.Remove;

        ResetLasersImmediate();
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

        StopAllSequences();
        SoundManager.Instance.PlaySE(SESoundData.SE.Pick);
        hasBuildBounds = false;
        currentBuildObject = null;
        removeTarget = null;
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
            NotifySequenceFinished();
            return;
        }

        StopCarryRoutine();
        carryRoutine = StartCoroutine(CoCommitCarrySequence());
    }

    public void CancelCarry()
    {
        StopCarryRoutine();
        moveTarget = null;
        EnterIdleState();
    }

    // ---------------------------------------------------------------------
    // State Updates
    // ---------------------------------------------------------------------

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

        Vector3 targetPos = reactWorldTarget + Vector3.up * reactHeightOffset + GetFloatOffset();
        MoveDrone(targetPos, reactMoveLerpSpeed);
        RotateDrone(reactWorldTarget);
    }

    private void UpdateBuild()
    {
        if (hasBuildBounds)
        {
            Vector3 targetPos = currentBuildBounds.center + buildHoverOffset + GetFloatOffset();
            MoveDrone(targetPos, buildMoveLerpSpeed);
            RotateDrone(currentBuildBounds.center);

            if (laserVisible)
                UpdateLaserToBounds(currentBuildBounds);
        }
        else
        {
            if (currentBuildObject == null)
                return;

            MoveToObject(currentBuildObject, buildMoveLerpSpeed);

            if (laserVisible)
                UpdateLaserToObject(currentBuildObject);
        }
    }

    private void UpdateRemove()
    {
        if (removeTarget == null)
            return;

        MoveToObject(removeTarget.gameObject, removeMoveLerpSpeed);

        if (laserVisible)
            UpdateLaserToObject(removeTarget.gameObject);
    }

    private void UpdateMove()
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

    // ---------------------------------------------------------------------
    // Build / Remove / Carry Sequences
    // ---------------------------------------------------------------------

    private IEnumerator CoBuildSequence()
    {
        yield return CoMoveUntilArrived(currentBuildObject, buildMoveLerpSpeed);

        if (currentBuildObject == null)
        {
            EnterIdleState();
            buildRoutine = null;
            NotifySequenceFinished();
            yield break;
        }
        SoundManager.Instance.PlaySE(SESoundData.SE.Place);
        SetRenderersVisible(currentBuildObject, true);
        PlayBuildEffect(currentBuildObject);

        yield return CoLaserActiveDuration(
            () => currentBuildObject != null,
            () => UpdateBuild()
        );

        currentBuildObject = null;
        buildRoutine = null;
        EnterIdleState();
        NotifySequenceFinished();
    }

    private IEnumerator CoBuildGroupSequence()
    {
        yield return CoMoveUntilArrivedBounds(currentBuildBounds, buildMoveLerpSpeed);

        if (currentBuildGroup.Count == 0)
        {
            hasBuildBounds = false;
            buildRoutine = null;
            EnterIdleState();
            NotifySequenceFinished();
            yield break;
        }
        SoundManager.Instance.PlaySE(SESoundData.SE.Place);

        for (int i = 0; i < currentBuildGroup.Count; i++)
        {
            GameObject go = currentBuildGroup[i];
            if (go == null)
                continue;

            SetRenderersVisible(go, true);
            PlayBuildEffect(go);
        }

        yield return CoLaserActiveDuration(
            () => hasBuildBounds,
            () => UpdateBuild()
        );

        currentBuildGroup.Clear();
        hasBuildBounds = false;
        buildRoutine = null;
        EnterIdleState();
        NotifySequenceFinished();
    }

    private IEnumerator CoBuildAtSequence(Vector3 worldPos)
    {
        float time = 0f;

        while (time < maxApproachTime)
        {
            Vector3 targetPos = worldPos + buildHoverOffset + GetFloatOffset();

            MoveDrone(targetPos, buildMoveLerpSpeed);
            RotateDrone(worldPos);

            if (Vector3.Distance(transform.position, targetPos) <= arrivalDistance)
                break;

            time += Time.deltaTime;
            yield return null;
        }

        buildRoutine = null;
        EnterIdleState();
        NotifySequenceFinished();
    }

    private IEnumerator CoRemoveSequence()
    {
        yield return CoMoveUntilArrived(removeTarget != null ? removeTarget.gameObject : null, removeMoveLerpSpeed);
        SoundManager.Instance.PlaySE(SESoundData.SE.Remove);

        if (removeTarget == null)
        {
            removeRoutine = null;
            EnterIdleState();
            NotifySequenceFinished();
            yield break;
        }

        laserVisible = true;
        ShowAllLasersImmediate();

        NotifySequenceFinished();

        float t = 0f;
        const float removeLaserDuration = 0.5f;

        while (t < removeLaserDuration)
        {
            if (removeTarget == null)
                break;

            UpdateRemove();
            FadeLasersToward(laserMaxAlpha);
            PulseLasers();

            t += Time.deltaTime;
            yield return null;
        }

        removeRoutine = null;
        EnterIdleState();
    }

    private IEnumerator CoCommitCarrySequence()
    {
        currentState = DroneState.Move;
        laserVisible = true;
        ShowAllLasersImmediate();
        SoundManager.Instance.PlaySE(SESoundData.SE.Drop);
        float t = 0f;
        while (t < carryCommitFadeDuration)
        {
            if (moveTarget == null)
                break;

            UpdateMove();

            float alpha = Mathf.Lerp(laserMaxAlpha, 0f, t / carryCommitFadeDuration);
            SetLaserAlpha(alpha);
            FadeLasersToward(alpha);
            PulseLasers();

            t += Time.deltaTime;
            yield return null;
        }

        moveTarget = null;
        carryRoutine = null;
        EnterIdleState();
        NotifySequenceFinished();
    }

    private IEnumerator CoLaserActiveDuration(Func<bool> keepRunning, Action updateAction)
    {
        laserVisible = true;
        ShowAllLasersImmediate();

        float time = 0f;
        while (time < buildDuration)
        {
            if (!keepRunning())
                break;

            updateAction?.Invoke();
            FadeLasersToward(laserMaxAlpha);
            PulseLasers();

            time += Time.deltaTime;
            yield return null;
        }

        float fadeTime = 0.12f;
        float t = 0f;

        while (t < fadeTime)
        {
            if (keepRunning())
                updateAction?.Invoke();

            float alpha = Mathf.Lerp(laserMaxAlpha, 0f, t / fadeTime);
            SetLaserAlpha(alpha);
            FadeLasersToward(alpha);
            PulseLasers();

            t += Time.deltaTime;
            yield return null;
        }
    }

    // ---------------------------------------------------------------------
    // Move Helpers
    // ---------------------------------------------------------------------

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

            if (Vector3.Distance(transform.position, targetPos) <= arrivalDistance)
                yield break;

            time += Time.deltaTime;
            yield return null;
        }
    }

    private IEnumerator CoMoveUntilArrivedBounds(Bounds bounds, float moveSpeed)
    {
        float time = 0f;

        while (time < maxApproachTime)
        {
            Vector3 targetPos = bounds.center + buildHoverOffset + GetFloatOffset();

            MoveDrone(targetPos, moveSpeed);
            RotateDrone(bounds.center);

            if (Vector3.Distance(transform.position, targetPos) <= arrivalDistance)
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
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * rotateSpeed);
    }

    // ---------------------------------------------------------------------
    // Target / Position Helpers
    // ---------------------------------------------------------------------

    private Vector3 GetIdleFollowPosition()
    {
        if (boyTarget == null)
            return transform.position;

        Vector3 worldOffset = boyTarget.TransformDirection(localOffset);
        return boyTarget.position + worldOffset + GetFloatOffset();
    }

    private Vector3 GetBuildHoverPosition(GameObject targetObject)
    {
        if (targetObject == null)
            return transform.position;

        if (TryGetObjectBounds(targetObject, out Bounds bounds))
            return bounds.center + buildHoverOffset + GetFloatOffset();

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

    // ---------------------------------------------------------------------
    // Laser
    // ---------------------------------------------------------------------

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

    private void ResetLasersImmediate()
    {
        laserVisible = false;
        HideAllLasersImmediate();
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

    private void UpdateLaserToObject(GameObject targetObject)
    {
        if (targetObject == null || !TryGetObjectBounds(targetObject, out Bounds bounds))
        {
            HideAllLasersImmediate();
            return;
        }

        UpdateLaserToBounds(bounds);
    }

    private void UpdateLaserToBounds(Bounds bounds)
    {
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

            start.a = Mathf.Lerp(start.a, targetAlpha, Time.deltaTime * laserAppearSpeed);
            end.a = Mathf.Lerp(end.a, targetAlpha, Time.deltaTime * laserAppearSpeed);

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

    private Vector3[] GetTopFourCorners(Bounds bounds)
    {
        Vector3 center = bounds.center;
        Vector3 ext = bounds.extents;
        float y = bounds.max.y + boundsTopSurfaceOffset;

        return new[]
        {
            new Vector3(center.x - ext.x, y, center.z - ext.z),
            new Vector3(center.x - ext.x, y, center.z + ext.z),
            new Vector3(center.x + ext.x, y, center.z - ext.z),
            new Vector3(center.x + ext.x, y, center.z + ext.z)
        };
    }

    // ---------------------------------------------------------------------
    // Bounds
    // ---------------------------------------------------------------------

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

    private bool TryGetMergedBounds(List<GameObject> objects, out Bounds merged)
    {
        merged = default;

        if (objects == null || objects.Count == 0)
            return false;

        bool found = false;

        for (int i = 0; i < objects.Count; i++)
        {
            GameObject go = objects[i];
            if (go == null)
                continue;

            if (!TryGetObjectBounds(go, out Bounds b))
                continue;

            if (!found)
            {
                merged = b;
                found = true;
            }
            else
            {
                merged.Encapsulate(b);
            }
        }

        return found;
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

    // ---------------------------------------------------------------------
    // Visual Helpers
    // ---------------------------------------------------------------------

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

    private void PlayBuildEffect(GameObject targetObject)
    {
        if (targetObject == null)
            return;

        BuildEffectUtility.PlayBuildEffect(targetObject);
    }

    // ---------------------------------------------------------------------
    // Internal State Helpers
    // ---------------------------------------------------------------------

    private void PrepareForBuildObject(GameObject spawnedObject)
    {
        StopAllSequences();

        hasBuildBounds = false;
        currentBuildObject = spawnedObject;
        removeTarget = null;
        moveTarget = null;
        currentState = DroneState.Build;

        ResetLasersImmediate();
        SetRenderersVisible(currentBuildObject, false);
    }

    private void PrepareForBuildBounds(Bounds mergedBounds)
    {
        StopAllSequences();

        currentBuildBounds = mergedBounds;
        hasBuildBounds = true;
        currentBuildObject = null;
        removeTarget = null;
        moveTarget = null;
        currentState = DroneState.Build;

        ResetLasersImmediate();
    }

    private void EnterIdleState()
    {
        currentState = DroneState.Idle;
        ResetLasersImmediate();
    }

    private void ClearTargets()
    {
        hasReactTarget = false;
        currentBuildObject = null;
        removeTarget = null;
        moveTarget = null;
        hasBuildBounds = false;
        currentBuildGroup.Clear();
    }
    private void NotifySequenceFinished()
    {
        SequenceFinished?.Invoke();
    }

    // ---------------------------------------------------------------------
    // Coroutine Stop Helpers
    // ---------------------------------------------------------------------

    private void StopAllSequences()
    {
        StopBuildRoutine();
        StopRemoveRoutine();
        StopCarryRoutine();
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