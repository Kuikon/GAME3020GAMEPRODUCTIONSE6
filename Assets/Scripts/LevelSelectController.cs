using System;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LevelSelectController : MonoBehaviour
{
    [Header("Scene Names")]
    [SerializeField] private string editorSceneName = "EditorAndPlay";

    [Header("Selected")]
    [SerializeField] private string selectedLevelId;

    [Header("List UI")]
    [SerializeField] private Transform listParent;
    [SerializeField] private LevelListItemUI listItemPrefab;

    [Header("Debug")]
    [SerializeField] private bool debugLogs = true;

    private LevelDB db;

    private void Awake()
    {
        db = new LevelDB();
    }

    private void Start()
    {
        EnsureGameManager();
        RefreshList();
    }

    // --------------------------------------------------
    // Buttons
    // --------------------------------------------------
    public void UI_New()
    {
        var meta = db.CreateNew("New Level");
        Select(meta.levelId);
        RefreshList();
        GoEditorEdit();
    }

    public void UI_Duplicate()
    {
        if (string.IsNullOrEmpty(selectedLevelId)) return;

        var meta = db.Duplicate(selectedLevelId);
        Select(meta.levelId);
        RefreshList();
    }

    public void UI_Delete()
    {
        if (string.IsNullOrEmpty(selectedLevelId)) return;

        db.Delete(selectedLevelId);
        selectedLevelId = "";
        RefreshList();
    }

    public void UI_Rename(string newName)
    {
        if (string.IsNullOrEmpty(selectedLevelId)) return;

        db.Rename(selectedLevelId, newName);
        RefreshList();
    }

    public void UI_Edit()
    {
        GoEditorEdit();
    }

    public void UI_Play()
    {
        GoEditorPlay();
    }

    public void UI_Refresh()
    {
        RefreshList();
    }

    // --------------------------------------------------
    // Helpers
    // --------------------------------------------------
    public void Select(string levelId)
    {
        selectedLevelId = levelId;

        if (debugLogs)
            Debug.Log("Selected: " + selectedLevelId);

        RefreshSelectionVisualOnly();
    }

    private void GoEditorEdit()
    {
        if (string.IsNullOrEmpty(selectedLevelId))
        {
            DebugLog("GoEditorEdit failed: no level selected.");
            return;
        }

        GameManager.I.CurrentLevelId = selectedLevelId;
        GameManager.I.StartMode = StartMode.Edit;
        SceneManager.LoadScene(editorSceneName);
    }

    private void GoEditorPlay()
    {
        if (string.IsNullOrEmpty(selectedLevelId))
        {
            DebugLog("GoEditorPlay failed: no level selected.");
            return;
        }

        GameManager.I.CurrentLevelId = selectedLevelId;
        GameManager.I.StartMode = StartMode.Play;
        SceneManager.LoadScene(editorSceneName);
    }

    public void RefreshList()
    {
        if (listParent == null)
        {
            Debug.LogWarning("LevelSelectController: listParent is not assigned.");
            return;
        }

        if (listItemPrefab == null)
        {
            Debug.LogWarning("LevelSelectController: listItemPrefab is not assigned.");
            return;
        }

        ClearList();

        var index = db.LoadIndex();
        if (index == null || index.levels == null)
        {
            DebugLog("RefreshList: index is null.");
            return;
        }

        for (int i = 0; i < index.levels.Count; i++)
        {
            var meta = index.levels[i];
            if (meta == null) continue;

            bool isSelected = meta.levelId == selectedLevelId;
            var item = Instantiate(listItemPrefab, listParent);
            item.Setup(meta, this, isSelected);
        }

        DebugLog($"RefreshList done. Count={index.levels.Count}");
    }

    private void RefreshSelectionVisualOnly()
    {
        if (listParent == null) return;

        for (int i = 0; i < listParent.childCount; i++)
        {
            var item = listParent.GetChild(i).GetComponent<LevelListItemUI>();
            if (item == null) continue;
        }

        // いちばん簡単に確実に更新したいなら再生成
        RefreshList();
    }

    private void ClearList()
    {
        for (int i = listParent.childCount - 1; i >= 0; i--)
        {
            Destroy(listParent.GetChild(i).gameObject);
        }
    }

    private void EnsureGameManager()
    {
        if (GameManager.I != null) return;

        var go = new GameObject("GameManager");
        go.AddComponent<GameManager>();
    }

    public void UI_PrintIndex()
    {
        var index = db.LoadIndex();
        Debug.Log($"Levels: {index.levels.Count}");

        foreach (var m in index.levels)
            Debug.Log($"{m.name} id={m.levelId} updated={new DateTime(m.updatedAtTicks):u}");
    }

    private void DebugLog(string msg)
    {
        if (debugLogs)
            Debug.Log(msg);
    }
}