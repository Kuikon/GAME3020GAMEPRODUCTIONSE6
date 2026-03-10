using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Cinemachine;

public class GameModeManager : MonoBehaviour
{
    public enum Mode { Play, Edit }

    [Header("Input")]
    [SerializeField] private InputActionAsset inputActions;
    [SerializeField] private string buildMapName = "Build";
    [SerializeField] private string toggleActionName = "ToggleMode";

    [Header("Refs")]
    [SerializeField] private RobotControllerCommander robot;
    [SerializeField] private OrbitCamera cameraOrbit;

    [Header("Build Scripts (enable in Edit)")]
    [SerializeField] private MonoBehaviour buildController;

    [Header("Play Camera")]
    [SerializeField] private Camera mainCamera; // Main Camera

    [Header("Edit Camera")]
    [SerializeField] private Camera editCamera;
    [SerializeField] private FreeFlyCamera editFly;

    [Header("Start")]
    [SerializeField] private Mode startMode = Mode.Edit;
    [SerializeField] private bool disableCameraLookInEdit = true;

    [Header("Placed Objects")]
    [SerializeField] private ObjectsDatabaseSO database;
    [SerializeField] private Transform placedRoot;

    [Header("Debug")]
    [SerializeField] private bool debugLogs = true;

    private Mode mode;

    private InputActionMap buildMap;
    private InputAction toggleAction;

    public Mode CurrentMode => mode;
    public bool IsPlayMode => mode == Mode.Play;
    public bool IsEditMode => mode == Mode.Edit;

    private void Awake()
    {
        if (inputActions != null)
        {
            buildMap = inputActions.FindActionMap(buildMapName, true);
            toggleAction = buildMap.FindAction(toggleActionName, true);
        }

        if (mainCamera == null)
            mainCamera = Camera.main;
    }

    private void OnEnable()
    {
        if (buildMap != null)
            buildMap.Enable();

        if (toggleAction != null)
            toggleAction.performed += OnToggle;
    }

    private void OnDisable()
    {
        if (toggleAction != null)
            toggleAction.performed -= OnToggle;

        if (buildMap != null)
            buildMap.Disable();
    }

    private void Start()
    {
        SetMode(startMode);
    }

    public void ForceModeEdit()
    {
        SetMode(Mode.Edit);
    }

    public void ForceModePlay()
    {
        SetMode(Mode.Play);
    }

    private void OnToggle(InputAction.CallbackContext ctx)
    {
        SetMode(mode == Mode.Play ? Mode.Edit : Mode.Play);
    }

    private void SetMode(Mode newMode)
    {
        mode = newMode;
        bool isPlay = (mode == Mode.Play);

        // =========================
        // Player control
        // =========================
        if (robot != null)
        {
            robot.SetInputEnabled(isPlay);

            if (!isPlay)
                robot.StopImmediately();
        }

        // =========================
        // Collider mode
        // =========================
        ApplyPlacedColliderMode(isPlay);

        // =========================
        // Orbit / look input
        // =========================
        if (cameraOrbit != null)
        {
            if (disableCameraLookInEdit)
                cameraOrbit.SetInputEnabled(isPlay);
            else
                cameraOrbit.SetInputEnabled(true);
        }

        // =========================
        // Build scripts
        // =========================
        if (buildController != null)
            buildController.enabled = !isPlay;


        // =========================
        // Real camera switching
        // =========================
        if (mainCamera != null)
            mainCamera.gameObject.SetActive(isPlay);

        if (editCamera != null)
            editCamera.gameObject.SetActive(!isPlay);

        // =========================
        // Edit free fly input
        // =========================
        if (editFly != null)
            editFly.SetInputEnabled(!isPlay);

        // =========================
        // Cursor
        // =========================
        Cursor.visible = !isPlay;
        Cursor.lockState = isPlay ? CursorLockMode.Locked : CursorLockMode.None;

        if (debugLogs)
        {
            Debug.Log($"[GameModeManager] Mode = {mode}");
            Debug.Log($"[GameModeManager] MainCamera Active = {(mainCamera != null && mainCamera.gameObject.activeSelf)}");
            Debug.Log($"[GameModeManager] EditCamera Active = {(editCamera != null && editCamera.gameObject.activeSelf)}");
        }
    }

    private void ApplyPlacedColliderMode(bool isPlay)
    {
        if (database == null) return;
        if (placedRoot == null) return;

        var blocks = placedRoot.GetComponentsInChildren<BlockInstance>(true);

        if (debugLogs)
            Debug.Log($"[ColliderSwitch] Found blocks: {blocks.Length}");

        foreach (var bi in blocks)
        {
            if (bi == null) continue;

            if (debugLogs)
                Debug.Log($"[ColliderSwitch] Applying to {bi.name}");

            bi.ApplyColliderMode(isPlay, database);
        }
    }
}