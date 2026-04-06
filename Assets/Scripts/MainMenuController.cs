using System;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuController : MonoBehaviour
{
    [Header("Scene Names")]
    [SerializeField] private string editorSceneName = "EditorAndPlay";
    [SerializeField] private string levelSelectSceneName = "LevelSelect";

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
    }

    // --------------------------------------------------
    // Create Level
    // --------------------------------------------------
    public void UI_CreateLevel()
    {
        var meta = db.CreateNew("New Level");

        if (meta == null)
        {
            DebugLog("CreateLevel failed: meta is null.");
            return;
        }

        GameManager.I.CurrentLevelId = meta.levelId;

        DebugLog($"[MainMenu] Create Level -> {meta.levelId}");

        SceneManager.LoadScene(editorSceneName);
    }

    // --------------------------------------------------
    // Stage Select
    // --------------------------------------------------
    public void UI_StageSelect()
    {
        DebugLog("[MainMenu] Open LevelSelect");
        SceneManager.LoadScene(levelSelectSceneName);
    }

    // --------------------------------------------------
    // Helpers
    // --------------------------------------------------
    private void EnsureGameManager()
    {
        if (GameManager.I != null) return;

        var go = new GameObject("GameManager");
        go.AddComponent<GameManager>();
    }

    private void DebugLog(string msg)
    {
        if (debugLogs)
            Debug.Log(msg);
    }
}