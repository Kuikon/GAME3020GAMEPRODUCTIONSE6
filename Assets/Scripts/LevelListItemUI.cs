using System;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LevelListItemUI : MonoBehaviour
{
    [Header("Texts")]
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text updatedText;

    [Header("Thumbnail")]
    [SerializeField] private Image thumbnailImage;

    [Header("Main Buttons")]
    [SerializeField] private Button selectButton;

    [Header("Per Item Buttons")]
    [SerializeField] private Button duplicateButton;
    [SerializeField] private Button deleteButton;
    [SerializeField] private Button renameButton;

    [Header("Rename UI")]
    [SerializeField] private TMP_InputField renameInput;

    private string levelId;
    private LevelSelectController owner;

    public void Setup(LevelMeta meta, LevelSelectController controller, bool isSelected)
    {
        levelId = meta.levelId;
        owner = controller;

        if (nameText != null)
            nameText.text = meta.name;

        if (updatedText != null)
        {
            DateTime dt = new DateTime(meta.updatedAtTicks, DateTimeKind.Utc).ToLocalTime();
            updatedText.text = $"Updated: {dt:yyyy-MM-dd HH:mm}";
        }

        if (renameInput != null)
            renameInput.text = meta.name;

        BindButtons();
        LoadThumbnail(meta.thumbnailPath);
    }

    private void BindButtons()
    {
        if (selectButton != null)
        {
            selectButton.onClick.RemoveAllListeners();
            selectButton.onClick.AddListener(UI_Select);
        }

        if (duplicateButton != null)
        {
            duplicateButton.onClick.RemoveAllListeners();
            duplicateButton.onClick.AddListener(UI_Duplicate);
        }

        if (deleteButton != null)
        {
            deleteButton.onClick.RemoveAllListeners();
            deleteButton.onClick.AddListener(UI_Delete);
        }

        if (renameButton != null)
        {
            renameButton.onClick.RemoveAllListeners();
            renameButton.onClick.AddListener(UI_Rename);
        }
    }

    private void LoadThumbnail(string path)
    {
        if (thumbnailImage == null) return;
        if (string.IsNullOrEmpty(path)) return;
        if (!File.Exists(path)) return;

        byte[] bytes = File.ReadAllBytes(path);

        Texture2D tex = new Texture2D(2, 2);
        if (!tex.LoadImage(bytes)) return;

        Sprite sprite = Sprite.Create(
            tex,
            new Rect(0, 0, tex.width, tex.height),
            new Vector2(0.5f, 0.5f)
        );

        thumbnailImage.sprite = sprite;
        thumbnailImage.preserveAspect = true;
    }

    public void UI_Select()
    {
        owner?.Select(levelId);
        owner.UI_Edit();
    }

    public void UI_Duplicate()
    {
        if (owner == null) return;

        owner.Select(levelId);
        owner.UI_Duplicate();
    }

    public void UI_Delete()
    {
        if (owner == null) return;

        owner.Select(levelId);
        owner.UI_Delete();
    }

    public void UI_Rename()
    {
        if (owner == null) return;
        if (renameInput == null) return;

        string newName = renameInput.text?.Trim();

        if (string.IsNullOrEmpty(newName))
            return;

        owner.Select(levelId);
        owner.UI_Rename(newName);
    }
}