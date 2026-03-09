using System;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LevelSelectController : MonoBehaviour
{
    [Header("Scene Names")]
    [SerializeField] private string editorSceneName = "EditorAndPlay";

    [Header("Selected")]
    [SerializeField] private string selectedLevelId; 

    private LevelDB db;

    private void Awake()
    {
        db = new LevelDB();
    }

    // ---- Buttons ----
    public void UI_New()
    {
        var meta = db.CreateNew("New Level");
        Select(meta.levelId);
        GoEditorEdit();
    }

    public void UI_Duplicate()
    {
        if (string.IsNullOrEmpty(selectedLevelId)) return;
        var meta = db.Duplicate(selectedLevelId);
        Select(meta.levelId);
    }

    public void UI_Delete()
    {
        if (string.IsNullOrEmpty(selectedLevelId)) return;
        db.Delete(selectedLevelId);
        selectedLevelId = "";
    }

    public void UI_Rename(string newName)
    {
        if (string.IsNullOrEmpty(selectedLevelId)) return;
        db.Rename(selectedLevelId, newName);
    }

    public void UI_Edit()
    {
        GoEditorEdit();
    }

    public void UI_Play()
    {
        GoEditorPlay();
    }

    // ---- helpers ----
    public void Select(string levelId)
    {
        selectedLevelId = levelId;
        Debug.Log("Selected: " + selectedLevelId);
    }

    private void GoEditorEdit()
    {
        if (string.IsNullOrEmpty(selectedLevelId)) return;
        GameManager.I.CurrentLevelId = selectedLevelId;
        GameManager.I.StartMode = StartMode.Edit;
        SceneManager.LoadScene(editorSceneName);
    }

    private void GoEditorPlay()
    {
        if (string.IsNullOrEmpty(selectedLevelId)) return;
        GameManager.I.CurrentLevelId = selectedLevelId;
        GameManager.I.StartMode = StartMode.Play;
        SceneManager.LoadScene(editorSceneName);
    }

    // デバッグ：コンソールで一覧を見る
    public void UI_PrintIndex()
    {
        var index = db.LoadIndex();
        Debug.Log($"Levels: {index.levels.Count}");
        foreach (var m in index.levels)
            Debug.Log($"{m.name} id={m.levelId} updated={new DateTime(m.updatedAtTicks):u}");
    }
}