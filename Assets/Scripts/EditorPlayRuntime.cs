using UnityEngine;

public class EditorPlayRuntime : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private GameModeManager modeManager;
    [SerializeField] private BuildController buildController;
    [SerializeField] private GridManager grid;
    [SerializeField] private ObjectsDatabaseSO database;
    [SerializeField] private Transform placedRoot;

    [Header("Thumbnail Camera")]
    [SerializeField] private Camera thumbnailCamera;
    [SerializeField] private int thumbnailWidth = 256;
    [SerializeField] private int thumbnailHeight = 144;

    [Header("Debug")]
    [SerializeField] private bool debugLogs = true;

    private LevelDB db;
    private LevelSerializer serializer;

    private void Awake()
    {
        db = new LevelDB();
        serializer = new LevelSerializer();

        if (grid == null)
            grid = FindFirstObjectByType<GridManager>();

        if (modeManager == null)
            modeManager = FindFirstObjectByType<GameModeManager>();

        if (buildController == null)
            buildController = FindFirstObjectByType<BuildController>();
    }

    private void Start()
    {
        EnsureGameManager();

        if (string.IsNullOrEmpty(GameManager.I.CurrentLevelId))
        {
            Debug.LogWarning("[EditorPlayRuntime] CurrentLevelId is empty. Creating temp level.");
            var meta = db.CreateNew("Auto Level");
            GameManager.I.CurrentLevelId = meta.levelId;
            GameManager.I.StartMode = StartMode.Edit;
        }

        LoadCurrentLevel();

        if (GameManager.I.StartMode == StartMode.Play)
            modeManager?.ForceModePlay();
        else
            modeManager?.ForceModeEdit();
    }

    private void EnsureGameManager()
    {
        if (GameManager.I != null) return;

        var go = new GameObject("GameManager");
        go.AddComponent<GameManager>();
    }

    public void UI_Save()
    {
        string levelId = GameManager.I.CurrentLevelId;

        var data = serializer.Capture(levelId, placedRoot);
        db.SaveLevel(data);

        SaveThumbnail(levelId);

        if (debugLogs)
            Debug.Log($"[EditorPlayRuntime] Saved Level: {levelId} / Blocks={data.blocks.Count}");
    }

    public void UI_Load()
    {
        LoadCurrentLevel();

        if (debugLogs)
            Debug.Log($"[EditorPlayRuntime] Loaded Level: {GameManager.I.CurrentLevelId}");
    }

    private void LoadCurrentLevel()
    {
        string levelId = GameManager.I.CurrentLevelId;
        var data = db.LoadLevel(levelId);

        serializer.Apply(
            data,
            placedRoot,
            grid,
            database,
            buildController != null ? buildController.Spawner : null,
            buildController != null ? buildController.Rules : null
        );
    }

    private void SaveThumbnail(string levelId)
    {
        if (thumbnailCamera == null)
        {
            Debug.LogWarning("[EditorPlayRuntime] Thumbnail camera is null.");
            return;
        }

        if (debugLogs)
            Debug.Log($"[EditorPlayRuntime] Thumbnail Camera = {thumbnailCamera.name}");

        RenderTexture rt = new RenderTexture(thumbnailWidth, thumbnailHeight, 24);
        Texture2D tex = new Texture2D(thumbnailWidth, thumbnailHeight, TextureFormat.RGB24, false);

        RenderTexture prevActive = RenderTexture.active;
        RenderTexture prevCameraTarget = thumbnailCamera.targetTexture;
        bool prevCameraEnabled = thumbnailCamera.enabled;

        // âÊñ Ç…èoÇ≥Ç∏ÅARenderTextureÇ…ÇæÇØï`âÊÇ∑ÇÈ
        thumbnailCamera.enabled = false;
        thumbnailCamera.targetTexture = rt;
        RenderTexture.active = rt;

        thumbnailCamera.Render();

        tex.ReadPixels(new Rect(0, 0, thumbnailWidth, thumbnailHeight), 0, 0);
        tex.Apply();

        // å≥Ç…ñﬂÇ∑
        thumbnailCamera.targetTexture = prevCameraTarget;
        thumbnailCamera.enabled = prevCameraEnabled;
        RenderTexture.active = prevActive;

        byte[] png = tex.EncodeToPNG();
        string thumbPath = db.GetThumbnailPath(levelId);
        System.IO.File.WriteAllBytes(thumbPath, png);

        db.SetThumbnailPath(levelId, thumbPath);

        Destroy(rt);
        Destroy(tex);

        if (debugLogs)
            Debug.Log($"[EditorPlayRuntime] Thumbnail saved: {thumbPath}");
    }
}