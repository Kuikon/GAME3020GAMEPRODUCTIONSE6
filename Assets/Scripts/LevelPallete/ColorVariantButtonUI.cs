using UnityEngine;
using UnityEngine.UI;

public class ColorVariantButtonUI : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private Button button;
    [SerializeField] private RawImage thumbnailImage;
    [SerializeField] private GameObject selectedFrame;

    public int ObjectID { get; private set; } = -1;
    public BlockColor Color { get; private set; }

    private BuildPaletteUI owner;

    public void Setup(
        int objectID,
        BlockColor color,
        Texture thumbnail,
        BuildPaletteUI paletteUI,
        bool selected)
    {
        ObjectID = objectID;
        Color = color;
        owner = paletteUI;

        if (thumbnailImage != null)
        {
            thumbnailImage.texture = thumbnail;
            thumbnailImage.enabled = thumbnail != null;

            if (thumbnail != null)
                thumbnailImage.color = UnityEngine.Color.white;
            else
                thumbnailImage.color = new Color(1f, 1f, 1f, 0f);
        }

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
        Texture icon = thumbnailImage != null ? thumbnailImage.texture : null;
        owner?.SelectItem(ObjectID, Color, icon);
    }

    public Texture GetIconTexture()
    {
        return thumbnailImage != null ? thumbnailImage.texture : null;
    }
}