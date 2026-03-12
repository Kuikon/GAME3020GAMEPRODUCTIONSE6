using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PaletteItemButtonUI : MonoBehaviour
{
    [SerializeField] private Button button;
    [SerializeField] private RawImage thumbnailImage;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private GameObject selectedFrame;

    public int ObjectID { get; private set; }

    private BuildPaletteUI owner;

    public void Setup(ObjectData data, Texture thumbnail, BuildPaletteUI paletteUI, bool selected)
    {
        owner = paletteUI;
        ObjectID = data.ID;

        if (thumbnailImage != null)
        {
            if (thumbnail != null)
            {
                thumbnailImage.texture = thumbnail;
                thumbnailImage.color = Color.white;
            }
            else
            {
                thumbnailImage.texture = null;
                thumbnailImage.color = new Color(1f, 1f, 1f, 0f);
            }
        }

        if (nameText != null)
            nameText.text = data.Name;

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
        owner?.SelectItem(ObjectID);
    }
}