using UnityEngine;

public class EditorPlayRuntime : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private GameModeManager modeManager;
    [SerializeField] private BuildController buildController;
    [SerializeField] private GridManager grid;
    [SerializeField] private ObjectsDatabaseSO database;
    [SerializeField] private Transform placedRoot;

    [Header("Debug")]
    [SerializeField] private bool debugLogs = true;

    private LevelDB db;
    private LevelSerializer serializer;
    private BuildSpawner spawner;
    private BuildPlacementRules rules;

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


        // Ç±ÇÃéûì_Ç≈éÊÇÍÇÍÇŒégÇ§
        if (buildController != null)
            rules = buildController.Rules;

        // Ç‹Çæ null Ç»ÇÁé©ëOê∂ê¨
        if (rules == null)
            rules = new BuildPlacementRules(grid);
    }

    private void Start()
    {
        EnsureGameManager();

        if (string.IsNullOrEmpty(GameManager.I.CurrentLevelId))
        {
            Debug.LogWarning("CurrentLevelId is empty. Creating temp level.");
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

        if (debugLogs)
            Debug.Log($"Saved Level: {levelId} / Blocks={data.blocks.Count}");
    }

    public void UI_Load()
    {
        LoadCurrentLevel();

        if (debugLogs)
            Debug.Log($"Loaded Level: {GameManager.I.CurrentLevelId}");
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
            spawner,
            rules

        );
    }
}