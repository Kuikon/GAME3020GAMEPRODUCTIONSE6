using UnityEngine;

public class PaletteWindowUI : MonoBehaviour
{
    [Header("Panels")]
    [SerializeField] private GameObject categoryPanel;
    [SerializeField] private GameObject itemPanel;
    [Header("Build System")]
    [SerializeField] private BuildController buildController;
    private bool isOpen = false;

    void Start()
    {
        Refresh();
    }

    public void Toggle()
    {
        isOpen = !isOpen;
        Refresh();
    }

    private void Refresh()
    {
        if (categoryPanel != null)
            categoryPanel.SetActive(isOpen);

        if (itemPanel != null)
            itemPanel.SetActive(isOpen);
        buildController.IsBuildEnabled = !isOpen;

        if (isOpen)
            buildController.CancelCurrentOperation();
    }
}