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
            GameObject prefab = data.GetPrefab(color);

            Texture thumbnail = null;
            if (thumbnailGenerator != null)
                thumbnail = thumbnailGenerator.GetThumbnail(data, color);

            bool isSelected = data.ID == selectedObjectID && color == selectedColor;

            ColorVariantButtonUI btn = Instantiate(colorVariantButtonPrefab, colorVariantRoot);
            btn.gameObject.SetActive(true);
            btn.Setup(data.ID, color, thumbnail, owner, isSelected);
            variantButtons.Add(btn);
        }
    }

    public void RefreshSelected(int selectedObjectID, BlockColor selectedColor)
    {
        for (int i = 0; i < variantButtons.Count; i++)
        {
            if (variantButtons[i] == null)
                continue;

            bool isSelected =
                variantButtons[i].ObjectID == selectedObjectID &&
                variantButtons[i].Color == selectedColor;

            variantButtons[i].SetSelected(isSelected);
        }
    }

    private List<BlockColor> GetAvailableColors(ObjectData data)
    {
        List<BlockColor> result = new List<BlockColor>();

        if (data == null)
            return result;

        if (data.HasColorVariant(BlockColor.Blue))
            result.Add(BlockColor.Blue);

        if (data.HasColorVariant(BlockColor.Red))
            result.Add(BlockColor.Red);

        if (data.HasColorVariant(BlockColor.Yellow))
            result.Add(BlockColor.Yellow);

        if (data.HasColorVariant(BlockColor.Green))
            result.Add(BlockColor.Green);

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