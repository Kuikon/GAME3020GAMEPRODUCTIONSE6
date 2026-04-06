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

    [Header("Select Button")]
    [SerializeField] private Button selectButton;

    [Header("Selected Visual")]
    [SerializeField] private GameObject selectedFrame;

    private string levelId;
    private LevelSelectController owner;

    public void Setup(LevelMeta meta, LevelSelectController controller, bool isSelected)
    {
        levelId = meta != null ? meta.levelId : "";
        owner = controller;

        if (meta == null)
        {
            ClearView();
            BindButton();
            return;
        }

        if (nameText != null)
            nameText.text = meta.name;

        if (updatedText != null)
        {
            DateTime dt = new DateTime(meta.updatedAtTicks, DateTimeKind.Utc).ToLocalTime();
            updatedText.text = $"Updated: {dt:yyyy-MM-dd HH:mm}";
        }

        if (selectedFrame != null)
            selectedFrame.SetActive(isSelected);

        LoadThumbnail(meta.thumbnailPath);
        BindButton();
    }

    private void BindButton()
    {
        if (selectButton == null) return;

        selectButton.onClick.RemoveAllListeners();
        selectButton.onClick.AddListener(UI_Select);
    }

    private void ClearView()
    {
        if (nameText != null)
            nameText.text = "No Level";

        if (updatedText != null)
            updatedText.text = "-";

        if (thumbnailImage != null)
            thumbnailImage.sprite = null;

        if (selectedFrame != null)
            selectedFrame.SetActive(false);
    }

    private void LoadThumbnail(string path)
    {
        if (thumbnailImage == null) return;

        thumbnailImage.sprite = null;

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
        Debug.Log($"LoadThumbnail path={path}");
    }

    public void UI_Select()
    {
        owner?.Select(levelId);
    }
}