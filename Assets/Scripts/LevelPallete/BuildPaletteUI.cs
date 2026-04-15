using System;
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
    [SerializeField] private CurrentSelectedBlockUI currentSelectedBlockUI;

    [Header("Initial")]
    [SerializeField] private ObjectCategory initialCategory = ObjectCategory.Ground;

    public ObjectCategory CurrentCategory { get; private set; }

    public int SelectedObjectID { get; private set; } = -1;
    public BlockColor SelectedColor { get; private set; } = BlockColor.Blue;

    private readonly List<CategoryButtonUI> categoryButtons = new();
    private readonly List<PaletteGroupItemUI> groupItems = new();
    private readonly List<ObjectCategory> categoryOrder = new();

    private int currentCategoryIndex = 0;

    private void Awake()
    {
        if (buildController == null)
            buildController = FindFirstObjectByType<BuildController>();

        if (thumbnailGenerator == null)
            thumbnailGenerator = FindFirstObjectByType<PrefabThumbnailGenerator>();
    }

    private void OnEnable()
    {
        if (buildController != null)
            buildController.OnSelectionChanged += HandleSelectionChanged;
    }

    private void OnDisable()
    {
        if (buildController != null)
            buildController.OnSelectionChanged -= HandleSelectionChanged;
    }

    private void Start()
    {
        BuildCategoryButtons();
        SetCategoryIndexFromCategory(initialCategory);
        SelectCategory(initialCategory);
    }

    // =========================
    // Category
    // =========================
    public void SelectCategory(ObjectCategory category)
    {
        CurrentCategory = category;
        SetCategoryIndexFromCategory(category);

        RefreshCategoryVisual();
        RebuildItemList();

        if (currentCategoryText != null)
            currentCategoryText.text = $" {CurrentCategory}";
    }

    public void SelectNextCategory(int direction)
    {
        if (categoryOrder.Count == 0)
            return;

        currentCategoryIndex += direction;

        if (currentCategoryIndex < 0)
            currentCategoryIndex = categoryOrder.Count - 1;
        else if (currentCategoryIndex >= categoryOrder.Count)
            currentCategoryIndex = 0;

        SelectCategory(categoryOrder[currentCategoryIndex]);
    }

    private void BuildCategoryButtons()
    {
        ClearChildren(categoryButtonRoot);
        categoryButtons.Clear();
        categoryOrder.Clear();

        ObjectCategory[] values = (ObjectCategory[])Enum.GetValues(typeof(ObjectCategory));

        foreach (var cat in values)
        {
            if (cat == ObjectCategory.None)
                continue;

            var btn = Instantiate(categoryButtonPrefab, categoryButtonRoot);
            btn.Setup(cat, this, cat == initialCategory);
            categoryButtons.Add(btn);
            categoryOrder.Add(cat);
        }
    }

    private void RefreshCategoryVisual()
    {
        foreach (var btn in categoryButtons)
        {
            if (btn == null)
                continue;

            btn.SetSelected(btn.Category == CurrentCategory);
        }
    }

    private void SetCategoryIndexFromCategory(ObjectCategory category)
    {
        currentCategoryIndex = categoryOrder.IndexOf(category);
        if (currentCategoryIndex < 0)
            currentCategoryIndex = 0;
    }

    // =========================
    // Item List
    // =========================
    private void RebuildItemList()
    {
        ClearChildren(itemRoot);
        groupItems.Clear();

        if (database == null)
        {
            RefreshCurrentSelectedIcon();
            return;
        }

        var list = database.GetByCategory(CurrentCategory);

        foreach (var data in list)
        {
            if (data == null)
                continue;

            var item = Instantiate(groupItemPrefab, itemRoot);
            item.Setup(data, this, thumbnailGenerator, SelectedObjectID, SelectedColor);
            groupItems.Add(item);
        }

        if (list.Count <= 0)
        {
            SelectedObjectID = -1;
            RefreshSelectedVisual();
            RefreshSelectedText();
            RefreshCurrentSelectedIcon();
            return;
        }

        bool currentSelectionStillExists = false;

        foreach (var data in list)
        {
            if (data != null && data.ID == SelectedObjectID)
            {
                currentSelectionStillExists = true;

                if (!data.HasColorVariant(SelectedColor))
                    SelectedColor = GetFirstAvailableColor(data);

                break;
            }
        }

        if (!currentSelectionStillExists)
        {
            var first = list[0];
            if (first != null)
            {
                SelectedObjectID = first.ID;
                SelectedColor = GetFirstAvailableColor(first);
            }
        }

        ApplySelectionToBuildController();
        RefreshSelectedVisual();
        RefreshSelectedText();
        RefreshCurrentSelectedIcon();
    }

    public void SelectNextItem(int direction)
    {
        if (database == null)
            return;

        var list = database.GetByCategory(CurrentCategory);
        if (list == null || list.Count == 0)
            return;

        int currentIndex = GetCurrentItemIndex(list);
        if (currentIndex < 0)
            currentIndex = 0;

        currentIndex += direction;

        if (currentIndex < 0)
            currentIndex = list.Count - 1;
        else if (currentIndex >= list.Count)
            currentIndex = 0;

        var nextData = list[currentIndex];
        if (nextData == null)
            return;

        BlockColor nextColor = nextData.HasColorVariant(SelectedColor)
            ? SelectedColor
            : GetFirstAvailableColor(nextData);

        Texture thumbnail = thumbnailGenerator != null
            ? thumbnailGenerator.GetThumbnail(nextData, nextColor)
            : null;

        SelectItem(nextData.ID, nextColor, thumbnail);
    }

    private int GetCurrentItemIndex(List<ObjectData> list)
    {
        for (int i = 0; i < list.Count; i++)
        {
            if (list[i] != null && list[i].ID == SelectedObjectID)
                return i;
        }

        return -1;
    }

    // =========================
    // Selection
    // =========================
    public void SelectItem(int objectID, BlockColor color, Texture icon)
    {
        SelectedObjectID = objectID;
        SelectedColor = color;

        ApplySelectionToBuildController();
        RefreshSelectedVisual();
        RefreshSelectedText();

        if (currentSelectedBlockUI != null)
        {
            if (icon != null)
                currentSelectedBlockUI.SetCurrentIcon(icon);
            else
                RefreshCurrentSelectedIcon();
        }
    }

    private void ApplySelectionToBuildController()
    {
        if (buildController == null)
            return;

        buildController.SetSelection(SelectedObjectID, SelectedColor);
    }

    private void HandleSelectionChanged(int objectID, BlockColor color)
    {
        SelectedObjectID = objectID;
        SelectedColor = color;

        RefreshSelectedVisual();
        RefreshSelectedText();
        RefreshCurrentSelectedIcon();
    }

    private void RefreshSelectedVisual()
    {
        foreach (var item in groupItems)
        {
            if (item == null)
                continue;

            item.RefreshSelected(SelectedObjectID, SelectedColor);
        }
    }

    private void RefreshSelectedText()
    {
        if (selectedItemText == null)
            return;

        if (database != null && database.TryGetByID(SelectedObjectID, out var data) && data != null)
            selectedItemText.text = $" {data.Name} ({SelectedColor})";
        else
            selectedItemText.text = "Selected: None";
    }

    private void RefreshCurrentSelectedIcon()
    {
        if (currentSelectedBlockUI == null)
            return;

        if (database == null)
        {
            currentSelectedBlockUI.ClearIcon();
            return;
        }

        if (!database.TryGetByID(SelectedObjectID, out var data) || data == null)
        {
            currentSelectedBlockUI.ClearIcon();
            return;
        }

        Texture thumbnail = thumbnailGenerator != null
            ? thumbnailGenerator.GetThumbnail(data, SelectedColor)
            : null;

        if (thumbnail != null)
            currentSelectedBlockUI.SetCurrentIcon(thumbnail);
        else
            currentSelectedBlockUI.ClearIcon();
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
        if (root == null)
            return;

        for (int i = root.childCount - 1; i >= 0; i--)
            Destroy(root.GetChild(i).gameObject);
    }
}