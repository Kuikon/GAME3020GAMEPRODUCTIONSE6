using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class BuildPaletteUI : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private ObjectsDatabaseSO database;
    [SerializeField] private BuildController buildController;
    [SerializeField] private PrefabThumbnailGenerator thumbnailGenerator;

    [Header("Category UI")]
    [SerializeField] private Transform categoryButtonRoot;
    [SerializeField] private CategoryButtonUI categoryButtonPrefab;

    [Header("Item List UI")]
    [SerializeField] private Transform itemRoot;
    [SerializeField] private PaletteGroupItemUI groupItemPrefab;

    [Header("Texts")]
    [SerializeField] private TMP_Text currentCategoryText;
    [SerializeField] private TMP_Text selectedItemText;

    [Header("Initial")]
    [SerializeField] private ObjectCategory initialCategory = ObjectCategory.Ground;

    public ObjectCategory CurrentCategory { get; private set; }

    public int SelectedObjectID { get; private set; } = -1;
    public BlockColor SelectedColor { get; private set; } = BlockColor.Blue;

    private readonly List<CategoryButtonUI> categoryButtons = new();
    private readonly List<PaletteGroupItemUI> groupItems = new();

    private void Awake()
    {
        if (buildController == null)
            buildController = FindFirstObjectByType<BuildController>();

        if (thumbnailGenerator == null)
            thumbnailGenerator = FindFirstObjectByType<PrefabThumbnailGenerator>();
    }

    private void Start()
    {
        BuildCategoryButtons();
        SelectCategory(initialCategory);
    }

    // =========================
    // Category
    // =========================
    public void SelectCategory(ObjectCategory category)
    {
        CurrentCategory = category;

        RefreshCategoryVisual();
        RebuildItemList();

        if (currentCategoryText != null)
            currentCategoryText.text = $" {CurrentCategory}";
    }

    private void BuildCategoryButtons()
    {
        ClearChildren(categoryButtonRoot);
        categoryButtons.Clear();

        ObjectCategory[] values = (ObjectCategory[])System.Enum.GetValues(typeof(ObjectCategory));

        foreach (var cat in values)
        {
            if (cat == ObjectCategory.None) continue;

            var btn = Instantiate(categoryButtonPrefab, categoryButtonRoot);
            btn.Setup(cat, this, cat == initialCategory);
            categoryButtons.Add(btn);
        }
    }

    private void RefreshCategoryVisual()
    {
        foreach (var btn in categoryButtons)
        {
            if (btn == null) continue;
            btn.SetSelected(btn.Category == CurrentCategory);
        }
    }

    // =========================
    // Item List
    // =========================
    private void RebuildItemList()
    {
        ClearChildren(itemRoot);
        groupItems.Clear();

        if (database == null) return;

        var list = database.GetByCategory(CurrentCategory);

        foreach (var data in list)
        {
            if (data == null) continue;

            var item = Instantiate(groupItemPrefab, itemRoot);
            item.Setup(data, this, thumbnailGenerator, SelectedObjectID, SelectedColor);
            groupItems.Add(item);
        }

        // ‰Šú‘I‘ð
        if (list.Count > 0 && SelectedObjectID == -1)
        {
            var first = list[0];
            if (first != null)
            {
                SelectedObjectID = first.ID;
                SelectedColor = GetFirstAvailableColor(first);
            }
        }

        RefreshSelectedVisual();
        RefreshSelectedText();
    }

    // =========================
    // Selection
    // =========================
    public void SelectItem(int objectID, BlockColor color)
    {
        SelectedObjectID = objectID;
        SelectedColor = color;

        if (buildController != null)
        {
            buildController.SetSelectedObject(objectID);
            buildController.SetSelectedColor(color); // ©’Ç‰Á•K—v
        }

        RefreshSelectedVisual();
        RefreshSelectedText();
    }

    private void RefreshSelectedVisual()
    {
        foreach (var item in groupItems)
        {
            if (item == null) continue;
            item.RefreshSelected(SelectedObjectID, SelectedColor);
        }
    }

    private void RefreshSelectedText()
    {
        if (selectedItemText == null) return;

        if (database != null && database.TryGetByID(SelectedObjectID, out var data) && data != null)
        {
            selectedItemText.text = $" {data.Name} ({SelectedColor})";
        }
        else
        {
            selectedItemText.text = "Selected: None";
        }
    }

    // =========================
    // Utils
    // =========================
    private BlockColor GetFirstAvailableColor(ObjectData data)
    {
        if (data.HasColorVariant(BlockColor.Blue)) return BlockColor.Blue;
        if (data.HasColorVariant(BlockColor.Red)) return BlockColor.Red;
        if (data.HasColorVariant(BlockColor.Yellow)) return BlockColor.Yellow;
        if (data.HasColorVariant(BlockColor.Green)) return BlockColor.Green;

        return BlockColor.Blue;
    }

    private void ClearChildren(Transform root)
    {
        if (root == null) return;

        for (int i = root.childCount - 1; i >= 0; i--)
            Destroy(root.GetChild(i).gameObject);
    }
}