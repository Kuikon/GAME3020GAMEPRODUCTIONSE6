using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CategoryButtonUI : MonoBehaviour
{
    [SerializeField] private Button button;
    [SerializeField] private TMP_Text labelText;
    [SerializeField] private GameObject selectedFrame;

    public ObjectCategory Category { get; private set; }

    private BuildPaletteUI owner;

    public void Setup(ObjectCategory category, BuildPaletteUI paletteUI, bool selected)
    {
        Category = category;
        owner = paletteUI;

        if (labelText != null)
            labelText.text = category.ToString();

        if (button != null)
        {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(OnClick);
        }

        SetSelected(selected);
    }

    public void SetSelected(bool selected)
    {
        if (selectedFrame != null)
            selectedFrame.SetActive(selected);
    }

    private void OnClick()
    {
        owner?.SelectCategory(Category);
    }
}