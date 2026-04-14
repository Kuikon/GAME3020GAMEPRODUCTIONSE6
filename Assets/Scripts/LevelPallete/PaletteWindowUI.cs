using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class PaletteWindowUI : MonoBehaviour
{
    [Header("Panels")]
    [SerializeField] private RectTransform categoryPanel;
    [SerializeField] private RectTransform itemPanel;

    [Header("Window Root")]
    [SerializeField] private RectTransform windowRoot;
    [SerializeField] private CanvasGroup itemCanvasGroup;

    [Header("Build System")]
    [SerializeField] private BuildController buildController;

    [Header("Animation")]
    [SerializeField] private float openDuration = 0.22f;
    [SerializeField] private float closeDuration = 0.18f;

    [Tooltip("Closed state Y position of WindowRoot")]
    [SerializeField] private float closedY = 0f;

    [Tooltip("Extra spacing between category and item area if needed")]
    [SerializeField] private float extraOpenOffset = 0f;

    [Header("Input")]
    [SerializeField] private InputActionReference togglePaletteAction;

    private bool isOpen = false;
    private Coroutine animationRoutine;
    private float openedY;

    private void Awake()
    {
        if (windowRoot == null)
            windowRoot = GetComponent<RectTransform>();

        if (itemCanvasGroup == null && itemPanel != null)
            itemCanvasGroup = itemPanel.GetComponent<CanvasGroup>();

        if (itemCanvasGroup == null && itemPanel != null)
            itemCanvasGroup = itemPanel.gameObject.AddComponent<CanvasGroup>();
    }

    private void OnEnable()
    {
        if (togglePaletteAction != null && togglePaletteAction.action != null)
        {
            togglePaletteAction.action.Enable();
            togglePaletteAction.action.performed += OnTogglePerformed;
        }
    }

    private void OnDisable()
    {
        if (togglePaletteAction != null && togglePaletteAction.action != null)
        {
            togglePaletteAction.action.performed -= OnTogglePerformed;
            togglePaletteAction.action.Disable();
        }
    }

    private void Start()
    {
        CalculateOpenedPosition();
        ForceClosedImmediate();
    }

    private void OnRectTransformDimensionsChange()
    {
        CalculateOpenedPosition();

        if (windowRoot == null)
            return;

        Vector2 pos = windowRoot.anchoredPosition;
        pos.y = isOpen ? openedY : closedY;
        windowRoot.anchoredPosition = pos;
    }

    private void OnTogglePerformed(InputAction.CallbackContext ctx)
    {
        Toggle();
    }

    public void Toggle()
    {
        SetOpen(!isOpen);
    }

    public void SetOpen(bool open)
    {
        if (isOpen == open)
            return;

        isOpen = open;

        if (animationRoutine != null)
            StopCoroutine(animationRoutine);

        if (isOpen)
        {
            if (buildController != null)
            {
                buildController.IsBuildEnabled = false;
                buildController.CancelCurrentOperation();
            }

            animationRoutine = StartCoroutine(CoAnimate(openedY, openDuration, true));
        }
        else
        {
            animationRoutine = StartCoroutine(CoAnimate(closedY, closeDuration, false));
        }
    }

    private void CalculateOpenedPosition()
    {
        if (itemPanel == null)
        {
            openedY = closedY;
            return;
        }

        openedY = itemPanel.rect.height + extraOpenOffset;
    }

    private IEnumerator CoAnimate(float targetY, float duration, bool opening)
    {
        if (windowRoot == null)
            yield break;

        if (itemPanel != null)
            itemPanel.gameObject.SetActive(true);

        if (itemCanvasGroup != null)
        {
            itemCanvasGroup.interactable = opening;
            itemCanvasGroup.blocksRaycasts = opening;
        }

        Vector2 startPos = windowRoot.anchoredPosition;
        Vector2 endPos = new Vector2(startPos.x, targetY);

        float startAlpha = itemCanvasGroup != null ? itemCanvasGroup.alpha : 1f;
        float endAlpha = opening ? 1f : 0f;

        float time = 0f;

        while (time < duration)
        {
            time += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(time / duration);

            float eased = opening ? EaseOutCubic(t) : EaseInCubic(t);

            windowRoot.anchoredPosition = Vector2.LerpUnclamped(startPos, endPos, eased);

            if (itemCanvasGroup != null)
                itemCanvasGroup.alpha = Mathf.Lerp(startAlpha, endAlpha, t);

            yield return null;
        }

        windowRoot.anchoredPosition = endPos;

        if (itemCanvasGroup != null)
        {
            itemCanvasGroup.alpha = endAlpha;
            itemCanvasGroup.interactable = opening;
            itemCanvasGroup.blocksRaycasts = opening;
        }

        if (!opening && itemPanel != null)
            itemPanel.gameObject.SetActive(false);

        if (!opening && buildController != null)
            buildController.IsBuildEnabled = true;

        animationRoutine = null;
    }

    private void ForceClosedImmediate()
    {
        isOpen = false;

        if (windowRoot != null)
        {
            Vector2 pos = windowRoot.anchoredPosition;
            pos.y = closedY;
            windowRoot.anchoredPosition = pos;
        }

        if (itemCanvasGroup != null)
        {
            itemCanvasGroup.alpha = 0f;
            itemCanvasGroup.interactable = false;
            itemCanvasGroup.blocksRaycasts = false;
        }

        if (itemPanel != null)
            itemPanel.gameObject.SetActive(false);

        if (buildController != null)
            buildController.IsBuildEnabled = true;
    }

    private float EaseOutCubic(float t)
    {
        return 1f - Mathf.Pow(1f - t, 3f);
    }

    private float EaseInCubic(float t)
    {
        return t * t * t;
    }
}