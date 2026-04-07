using UnityEngine;
using UnityEngine.InputSystem;

public class PaletteInputController : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private BuildPaletteUI paletteUI;
    [SerializeField] private GameObject paletteRootObject;

    [Header("Input Actions")]
    [SerializeField] private InputActionReference navigateCategoryAction;
    [SerializeField] private InputActionReference scrollItemAction;

    [Header("Options")]
    [SerializeField] private bool onlyWhenPaletteVisible = true;
    [SerializeField] private float scrollThreshold = 0.01f;

    private void OnEnable()
    {
        if (navigateCategoryAction != null)
        {
            navigateCategoryAction.action.Enable();
            navigateCategoryAction.action.performed += OnNavigateCategory;
        }

        if (scrollItemAction != null)
        {
            scrollItemAction.action.Enable();
            scrollItemAction.action.performed += OnScrollItem;
        }
    }

    private void OnDisable()
    {
        if (navigateCategoryAction != null)
        {
            navigateCategoryAction.action.performed -= OnNavigateCategory;
            navigateCategoryAction.action.Disable();
        }

        if (scrollItemAction != null)
        {
            scrollItemAction.action.performed -= OnScrollItem;
            scrollItemAction.action.Disable();
        }
    }

    private bool CanAcceptInput()
    {
        if (paletteUI == null)
            return false;

        if (!onlyWhenPaletteVisible)
            return true;

        if (paletteRootObject == null)
            return true;

        return paletteRootObject.activeInHierarchy;
    }

    private void OnNavigateCategory(InputAction.CallbackContext context)
    {
        if (!CanAcceptInput())
            return;

        Vector2 value = context.ReadValue<Vector2>();

        if (value.x > 0.5f)
            paletteUI.SelectNextCategory(+1);
        else if (value.x < -0.5f)
            paletteUI.SelectNextCategory(-1);
    }

    private void OnScrollItem(InputAction.CallbackContext context)
    {
        if (!CanAcceptInput())
            return;

        float value = context.ReadValue<float>();

        if (value > scrollThreshold)
        {
            // wheel up
            paletteUI.SelectNextItem(+1);
        }
        else if (value < -scrollThreshold)
        {
            // wheel down
            paletteUI.SelectNextItem(-1);
        }
    }
}