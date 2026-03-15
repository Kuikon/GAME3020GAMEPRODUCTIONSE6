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

    [Header("Item Grid UI")]
    [SerializeField] private Transform itemButtonRoot;
    [SerializeField] private PaletteItemButtonUI itemButtonPrefab;

    [Header("Texts")]
    [SerializeField] private TMP_Text currentCategoryText;
    [SerializeField] private TMP_Text selectedItemText;

    [Header("Initial")]
    [SerializeField] private ObjectCategory initialCategory = ObjectCategory.Ground;
    [SerializeField] private bool warmupThumbnailsOnStart = true;

    public ObjectCategory CurrentCategory { get; private set; }
    public int SelectedObjectID { get; private set; } = -1;

    private readonly List<CategoryButtonUI> categoryButtons = new List<CategoryButtonUI>();
    private readonly List<PaletteItemButtonUI> itemButtons = new List<PaletteItemButtonUI>();

    private void Awake()
    {
        if (buildController == null)
            buildController = FindFirstObjectByType<BuildController>();

        if (thumbnailGenerator == null)
            thumbnailGenerator = FindFirstObjectByType<PrefabThumbnailGenerator>();
    }

    private void Start()
    {
        if (warmupThumbnailsOnStart && thumbnailGenerator != null && database != null)
            thumbnailGenerator.Warmup(database);

        BuildCategoryButtons();
        SelectCategory(initialCategory);
    }

    public void SelectCategory(ObjectCategory category)
    {
        CurrentCategory = category;

        RefreshCategoryVisual();
        RebuildItemGrid();

        if (currentCategoryText != null)
            currentCategoryText.text = $" {CurrentCategory}";
    }

    public void SelectItem(int objectID)
    {
        SelectedObjectID = objectID;

        if (buildController != null)
            buildController.SetSelectedObject(objectID);

        RefreshItemVisual();
        RefreshSelectedItemText();
    }

    private void BuildCategoryButtons()
    {
        ClearChildren(categoryButtonRoot);
        categoryButtons.Clear();

        ObjectCategory[] values = (ObjectCategory[])System.Enum.GetValues(typeof(ObjectCategory));

        for (int i = 0; i < values.Length; i++)
        {
            ObjectCategory cat = values[i];
            if (cat == ObjectCategory.None) continue;

            CategoryButtonUI btn = Instantiate(categoryButtonPrefab, categoryButtonRoot);
            btn.Setup(cat, this, cat == initialCategory);
            categoryButtons.Add(btn);
        }
    }

    private void RebuildItemGrid()
    {
        ClearChildren(itemButtonRoot);
        itemButtons.Clear();

        if (database == null) return;

        List<ObjectData> list = database.GetByCategory(CurrentCategory);

        for (int i = 0; i < list.Count; i++)
        {
            ObjectData data = list[i];
            if (data == null) continue;

            bool isSelected = data.ID == SelectedObjectID;
            Texture thumbnail = thumbnailGenerator != null ? thumbnailGenerator.GetThumbnail(data) : null;

            PaletteItemButtonUI btn = Instantiate(itemButtonPrefab, itemButtonRoot);
            btn.Setup(data, thumbnail, this, isSelected);
            itemButtons.Add(btn);
        }

        bool hasCurrent = false;
        for (int i = 0; i < list.Count; i++)
        {
            if (list[i] != null && list[i].ID == SelectedObjectID)
            {
                hasCurrent = true;
                break;
            }
        }

        if (!hasCurrent)
        {
            if (list.Count > 0 && list[0] != null)
                SelectItem(list[0].ID);
            else
                SelectedObjectID = -1;
        }

        RefreshSelectedItemText();
    }

    private void RefreshCategoryVisual()
    {
        for (int i = 0; i < categoryButtons.Count; i++)
        {
            if (categoryButtons[i] == null) continue;
            categoryButtons[i].SetSelected(categoryButtons[i].Category == CurrentCategory);
        }
    }

    private void RefreshItemVisual()
    {
        for (int i = 0; i < itemButtons.Count; i++)
        {
            if (itemButtons[i] == null) continue;
            itemButtons[i].SetSelected(itemButtons[i].ObjectID == SelectedObjectID);
        }
    }

    private void RefreshSelectedItemText()
    {
        if (selectedItemText == null) return;

        if (database != null && database.TryGetByID(SelectedObjectID, out var data) && data != null)
            selectedItemText.text = $" {data.Name}";
        else
            selectedItemText.text = "Selected: None";
    }

    private void ClearChildren(Transform root)
    {
        if (root == null) return;

        for (int i = root.childCount - 1; i >= 0; i--)
            Destroy(root.GetChild(i).gameObject);
    }
}