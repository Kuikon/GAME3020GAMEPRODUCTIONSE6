using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using TMPro;

public class LevelSelectController : MonoBehaviour
{
    [Header("Scene Names")]
    [SerializeField] private string editorSceneName = "EditorAndPlay";

    [Header("Single Item Spawn UI")]
    [SerializeField] private Transform listParent;
    [SerializeField] private LevelListItemUI listItemPrefab;

    [Header("Rename UI")]
    [SerializeField] private TMP_InputField renameInput;

    [Header("Page Text")]
    [SerializeField] private TMP_Text pageText;

    [Header("Input")]
    [SerializeField] private bool useKeyboardArrow = true;
    [SerializeField] private bool useGamepadDPad = true;
    [SerializeField] private float inputCooldown = 0.2f;

    [Header("Selected")]
    [SerializeField] private string selectedLevelId;

    [Header("Debug")]
    [SerializeField] private bool debugLogs = true;

    private LevelDB db;
    private LevelIndex cachedIndex;
    private int currentIndex = 0;
    private float nextInputTime = 0f;

    private void Awake()
    {
        db = new LevelDB();
        SoundManager.Instance.PlayBGM(BGMSoundData.BGM.StageSelect);
    }

    private void Start()
    {
        EnsureGameManager();
        RefreshList();
    }

    private void Update()
    {
        HandlePageInput();
    }

    // --------------------------------------------------
    // Common Buttons
    // --------------------------------------------------
    public void UI_New()
    {
        var meta = db.CreateNew("New Level");
        RefreshList();
        Select(meta.levelId);
    }

    public void UI_Play()
    {
        GoEditorEdit();
    }

    public void UI_Duplicate()
    {
        var meta = GetCurrentMeta();
        if (meta == null) return;

        var duplicated = db.Duplicate(meta.levelId);
        RefreshList();
        Select(duplicated.levelId);
    }

    public void UI_Delete()
    {
        var meta = GetCurrentMeta();
        if (meta == null) return;

        db.Delete(meta.levelId);
        RefreshList();

        if (cachedIndex == null || cachedIndex.levels == null || cachedIndex.levels.Count == 0)
        {
            selectedLevelId = "";
            currentIndex = 0;
            RefreshCurrentItemOnly();
            return;
        }

        currentIndex = Mathf.Clamp(currentIndex, 0, cachedIndex.levels.Count - 1);
        selectedLevelId = cachedIndex.levels[currentIndex].levelId;
        RefreshCurrentItemOnly();
    }

    public void UI_Rename()
    {
        var meta = GetCurrentMeta();
        if (meta == null) return;
        if (renameInput == null) return;

        string newName = renameInput.text?.Trim();
        if (string.IsNullOrEmpty(newName)) return;

        db.Rename(meta.levelId, newName);
        RefreshList();
        Select(meta.levelId);
    }

    public void UI_Refresh()
    {
        RefreshList();
    }

    public void UI_PrevPage()
    {
        MovePage(-1);
    }

    public void UI_NextPage()
    {
        MovePage(+1);
    }

    // --------------------------------------------------
    // Core
    // --------------------------------------------------
    public void RefreshList()
    {
        cachedIndex = db.LoadIndex();

        if (cachedIndex == null || cachedIndex.levels == null)
        {
            DebugLog("RefreshList: index is null.");
            cachedIndex = new LevelIndex();
        }

        if (cachedIndex.levels.Count == 0)
        {
            selectedLevelId = "";
            currentIndex = 0;
            RefreshCurrentItemOnly();
            return;
        }

        if (string.IsNullOrEmpty(selectedLevelId))
        {
            currentIndex = Mathf.Clamp(currentIndex, 0, cachedIndex.levels.Count - 1);
            selectedLevelId = cachedIndex.levels[currentIndex].levelId;
        }
        else
        {
            int found = FindIndexByLevelId(selectedLevelId);
            if (found >= 0)
            {
                currentIndex = found;
            }
            else
            {
                currentIndex = Mathf.Clamp(currentIndex, 0, cachedIndex.levels.Count - 1);
                selectedLevelId = cachedIndex.levels[currentIndex].levelId;
            }
        }

        RefreshCurrentItemOnly();
        DebugLog($"RefreshList done. Count={cachedIndex.levels.Count}");
    }

    public void Select(string levelId)
    {
        if (string.IsNullOrEmpty(levelId)) return;

        int found = FindIndexByLevelId(levelId);
        if (found < 0) return;

        currentIndex = found;
        selectedLevelId = levelId;

        RefreshCurrentItemOnly();
        DebugLog($"Selected: {selectedLevelId}, index={currentIndex}");
    }

    private void MovePage(int dir)
    {
        if (cachedIndex == null || cachedIndex.levels == null) return;
        if (cachedIndex.levels.Count == 0) return;

        currentIndex += dir;

        if (currentIndex < 0)
            currentIndex = cachedIndex.levels.Count - 1;
        else if (currentIndex >= cachedIndex.levels.Count)
            currentIndex = 0;

        selectedLevelId = cachedIndex.levels[currentIndex].levelId;
        RefreshCurrentItemOnly();

        DebugLog($"MovePage -> currentIndex={currentIndex}, selected={selectedLevelId}");
    }

    private void RefreshCurrentItemOnly()
    {
        ClearList();

        var meta = GetCurrentMeta();

        if (meta != null && listParent != null && listItemPrefab != null)
        {
            var item = Instantiate(listItemPrefab, listParent);
            item.Setup(meta, this, true);
        }

        if (renameInput != null)
            renameInput.text = meta != null ? meta.name : "";

        if (pageText != null)
        {
            int total = (cachedIndex != null && cachedIndex.levels != null) ? cachedIndex.levels.Count : 0;
            if (meta == null || total == 0)
                pageText.text = "0 / 0";
            else
                pageText.text = $"{currentIndex + 1} / {total}";
        }
    }

    private LevelMeta GetCurrentMeta()
    {
        if (cachedIndex == null || cachedIndex.levels == null) return null;
        if (cachedIndex.levels.Count == 0) return null;
        if (currentIndex < 0 || currentIndex >= cachedIndex.levels.Count) return null;

        return cachedIndex.levels[currentIndex];
    }

    private int FindIndexByLevelId(string levelId)
    {
        if (cachedIndex == null || cachedIndex.levels == null) return -1;

        for (int i = 0; i < cachedIndex.levels.Count; i++)
        {
            var meta = cachedIndex.levels[i];
            if (meta != null && meta.levelId == levelId)
                return i;
        }

        return -1;
    }

    private void ClearList()
    {
        if (listParent == null) return;

        for (int i = listParent.childCount - 1; i >= 0; i--)
        {
            Destroy(listParent.GetChild(i).gameObject);
        }
    }

    private void GoEditorEdit()
    {
        var meta = GetCurrentMeta();
        if (meta == null)
        {
            DebugLog("GoEditorEdit failed: no level selected.");
            return;
        }

        GameManager.I.CurrentLevelId = meta.levelId;
        SceneManager.LoadScene(editorSceneName);
    }

    private void HandlePageInput()
    {
        if (Time.unscaledTime < nextInputTime)
            return;

        bool leftPressed = false;
        bool rightPressed = false;

        if (useKeyboardArrow && Keyboard.current != null)
        {
            leftPressed |= Keyboard.current.leftArrowKey.wasPressedThisFrame;
            rightPressed |= Keyboard.current.rightArrowKey.wasPressedThisFrame;
        }

        if (useGamepadDPad && Gamepad.current != null)
        {
            leftPressed |= Gamepad.current.dpad.left.wasPressedThisFrame;
            rightPressed |= Gamepad.current.dpad.right.wasPressedThisFrame;
        }

        if (leftPressed)
        {
            MovePage(-1);
            nextInputTime = Time.unscaledTime + inputCooldown;
        }
        else if (rightPressed)
        {
            MovePage(+1);
            nextInputTime = Time.unscaledTime + inputCooldown;
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
        if (index == null || index.levels == null)
        {
            Debug.Log("Levels: 0");
            return;
        }

        Debug.Log($"Levels: {index.levels.Count}");

        foreach (var m in index.levels)
        {
            if (m == null) continue;
            Debug.Log($"{m.name} id={m.levelId} updated={new DateTime(m.updatedAtTicks):u}");
        }
    }

    private void DebugLog(string msg)
    {
        if (debugLogs)
            Debug.Log(msg);
    }
}