using System;
using System.Collections;
using UnityEngine;

public class BlockBuildEffect : MonoBehaviour
{
    [Header("Build Motion")]
    [SerializeField] private float buildDuration = 0.18f;
    [SerializeField] private float riseDistance = 0.35f;
    [SerializeField] private float startScaleMultiplier = 0.15f;
    [SerializeField] private AnimationCurve buildCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Header("Destroy Motion")]
    [SerializeField] private float destroyDuration = 0.16f;
    [SerializeField] private float shrinkMultiplier = 0.1f;
    [SerializeField] private float sinkDistance = 0.25f;
    [SerializeField] private AnimationCurve destroyCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    private Coroutine playingRoutine;

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
            colliders[i].enabled = false;

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
}