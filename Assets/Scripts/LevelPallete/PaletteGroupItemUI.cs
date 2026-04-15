using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class PaletteGroupItemUI : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private Transform colorVariantRoot;
    [SerializeField] private ColorVariantButtonUI colorVariantButtonPrefab;

    private readonly List<ColorVariantButtonUI> variantButtons = new List<ColorVariantButtonUI>();

    public int ObjectID { get; private set; } = -1;

    public void Setup(
        ObjectData data,
        BuildPaletteUI owner,
        PrefabThumbnailGenerator thumbnailGenerator,
        int selectedObjectID,
        BlockColor selectedColor)
    {
        ObjectID = data != null ? data.ID : -1;

        if (nameText != null)
            nameText.text = data != null ? data.Name : "None";

        ClearChildren(colorVariantRoot);
        variantButtons.Clear();

        if (data == null || colorVariantRoot == null || colorVariantButtonPrefab == null)
            return;

        List<BlockColor> colors = GetAvailableColors(data);

        for (int i = 0; i < colors.Count; i++)
        {
            BlockColor color = colors[i];

            Texture thumbnail = thumbnailGenerator != null
                ? thumbnailGenerator.GetThumbnail(data, color)
                : null;

            var buttonUI = Instantiate(colorVariantButtonPrefab, colorVariantRoot);

            bool isSelected = (data.ID == selectedObjectID && color == selectedColor);

            buttonUI.Setup(
                data.ID,
                color,
                thumbnail,
                owner,
                isSelected
            );

            variantButtons.Add(buttonUI);
        }
    }

    public void RefreshSelected(int selectedObjectID, BlockColor selectedColor)
    {
        for (int i = 0; i < variantButtons.Count; i++)
        {
            var buttonUI = variantButtons[i];
            if (buttonUI == null)
                continue;

            bool isSelected =
                buttonUI.ObjectID == selectedObjectID &&
                buttonUI.Color == selectedColor;

            buttonUI.SetSelected(isSelected);
        }
    }

    private List<BlockColor> GetAvailableColors(ObjectData data)
    {
        List<BlockColor> result = new List<BlockColor>();

        if (data == null)
            return result;

        if (data.HasExactColorVariant(BlockColor.Blue)) result.Add(BlockColor.Blue);
        if (data.HasExactColorVariant(BlockColor.Red)) result.Add(BlockColor.Red);
        if (data.HasExactColorVariant(BlockColor.Yellow)) result.Add(BlockColor.Yellow);
        if (data.HasExactColorVariant(BlockColor.Green)) result.Add(BlockColor.Green);

        if (result.Count == 0 && data.Prefab != null)
            result.Add(BlockColor.Blue);

        return result;
    }

    private void ClearChildren(Transform root)
    {
        if (root == null)
            return;

        for (int i = root.childCount - 1; i >= 0; i--)
            Destroy(root.GetChild(i).gameObject);
    }
}