using System;
using System.Collections;
using UnityEngine;

public class BlockBuildEffect : MonoBehaviour
{
    [Header("Build Motion")]
    [SerializeField] private float buildDuration = 0.8f;
    [SerializeField] private float riseDistance = 0.35f;
    [SerializeField] private float startScaleMultiplier = 0.15f;
    [SerializeField] private AnimationCurve buildCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Header("Destroy Motion")]
    [SerializeField] private float destroyDuration = 0.6f;
    [SerializeField] private float shrinkMultiplier = 0.1f;
    [SerializeField] private float sinkDistance = 0.25f;
    [SerializeField] private AnimationCurve destroyCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Header("Pickup Motion")]
    [SerializeField] private float pickupDuration = 0.18f;
    [SerializeField] private float pickupRiseDistance = 0.12f;
    [SerializeField] private float pickupScaleMultiplier = 0.9f;
    [SerializeField] private AnimationCurve pickupCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Header("Drop Motion")]
    [SerializeField] private float dropDuration = 0.18f;
    [SerializeField] private float dropStartHeight = 0.12f;
    [SerializeField] private float dropStartScaleMultiplier = 0.9f;
    [SerializeField] private AnimationCurve dropCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    private Coroutine playingRoutine;
    private Vector3 originalScale;

    private void Awake()
    {
        originalScale = transform.localScale;
    }
    public void PlayBuild()
    {
        if (playingRoutine != null)
            StopCoroutine(playingRoutine);

        gameObject.SetActive(true);
        playingRoutine = StartCoroutine(CoPlayBuild());
    }

    public void PlayDestroy(Action onComplete = null)
    {
        if (playingRoutine != null)
            StopCoroutine(playingRoutine);

        playingRoutine = StartCoroutine(CoPlayDestroy(onComplete));
    }

    public void PlayPickup()
    {
        if (playingRoutine != null)
            StopCoroutine(playingRoutine);

        gameObject.SetActive(true);
        playingRoutine = StartCoroutine(CoPlayPickup());
    }

    public void PlayDrop()
    {
        if (playingRoutine != null)
            StopCoroutine(playingRoutine);

        gameObject.SetActive(true);
        playingRoutine = StartCoroutine(CoPlayDrop());
    }

    private IEnumerator CoPlayBuild()
    {
        Vector3 finalPosition = transform.position;
        Vector3 finalScale = transform.localScale;

        Vector3 startPosition = finalPosition + Vector3.down * riseDistance;
        Vector3 startScale = finalScale * startScaleMultiplier;

        transform.position = startPosition;
        transform.localScale = startScale;

        float time = 0f;

        while (time < buildDuration)
        {
            time += Time.deltaTime;
            float t = Mathf.Clamp01(time / buildDuration);
            float eased = buildCurve.Evaluate(t);

            transform.position = Vector3.LerpUnclamped(startPosition, finalPosition, eased);
            transform.localScale = Vector3.LerpUnclamped(startScale, finalScale, eased);

            yield return null;
        }

        transform.position = finalPosition;
        transform.localScale = finalScale;
        playingRoutine = null;
    }

    private IEnumerator CoPlayDestroy(Action onComplete)
    {
        Vector3 startPosition = transform.position;
        Vector3 startScale = transform.localScale;

        Vector3 endPosition = startPosition + Vector3.down * sinkDistance;
        Vector3 endScale = startScale * shrinkMultiplier;

        Collider[] colliders = GetComponentsInChildren<Collider>();
        for (int i = 0; i < colliders.Length; i++)
        {
            if (colliders[i] != null)
                colliders[i].enabled = false;
        }

        float time = 0f;

        while (time < destroyDuration)
        {
            time += Time.deltaTime;
            float t = Mathf.Clamp01(time / destroyDuration);
            float eased = destroyCurve.Evaluate(t);

            transform.position = Vector3.LerpUnclamped(startPosition, endPosition, eased);
            transform.localScale = Vector3.LerpUnclamped(startScale, endScale, eased);

            yield return null;
        }

        transform.position = endPosition;
        transform.localScale = endScale;
        playingRoutine = null;

        onComplete?.Invoke();
    }

    private IEnumerator CoPlayPickup()
    {
        Vector3 startPosition = transform.position;
        Vector3 startScale = originalScale;

        Vector3 endPosition = startPosition + Vector3.up * pickupRiseDistance;
        Vector3 endScale = startScale * pickupScaleMultiplier;

        float time = 0f;

        while (time < pickupDuration)
        {
            time += Time.deltaTime;
            float t = Mathf.Clamp01(time / pickupDuration);
            float eased = pickupCurve.Evaluate(t);

            transform.position = Vector3.LerpUnclamped(startPosition, endPosition, eased);
            transform.localScale = Vector3.LerpUnclamped(startScale, endScale, eased);

            yield return null;
        }

        transform.position = endPosition;
        transform.localScale = endScale;
        playingRoutine = null;
    }

    private IEnumerator CoPlayDrop()
    {
        Vector3 finalPosition = transform.position;
        Vector3 finalScale = originalScale;

        Vector3 startPosition = finalPosition + Vector3.up * dropStartHeight;
        Vector3 startScale = finalScale * dropStartScaleMultiplier;

        transform.position = startPosition;
        transform.localScale = startScale;

        float time = 0f;

        while (time < dropDuration)
        {
            time += Time.deltaTime;
            float t = Mathf.Clamp01(time / dropDuration);
            float eased = dropCurve.Evaluate(t);

            transform.position = Vector3.LerpUnclamped(startPosition, finalPosition, eased);
            transform.localScale = Vector3.LerpUnclamped(startScale, finalScale, eased);

            yield return null;
        }

        transform.position = finalPosition;
        transform.localScale = finalScale;
        playingRoutine = null;
    }
}