using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Cinemachine;

public class GameModeManager : MonoBehaviour
{
    public enum Mode { Play, Edit }

    [Header("Input")]
    [SerializeField] private InputActionAsset inputActions;
    [SerializeField] private string buildMapName = "Build";
    [SerializeField] private string gameModeName = "GameMode";
    [SerializeField] private string toggleActionName = "ToggleMode";

    [Header("Refs")]
    [SerializeField] private RobotControllerCommander robot;
    [SerializeField] private OrbitCamera cameraOrbit;
    [SerializeField] private LevelRuntimeCoordinator runtimeCoordinator;

    [Header("Build Scripts (enable in Edit)")]
    [SerializeField] private MonoBehaviour buildController;

    [Header("Play Camera")]
    [SerializeField] private Camera mainCamera;

    [Header("Edit Camera")]
    [SerializeField] private Camera editCamera;

    [Header("Start")]
    [SerializeField] private bool disableCameraLookInEdit = true;

    [Header("UI")]
    [SerializeField] private GameObject normalUIRoot; 
    [SerializeField] private GameObject backButton;  
    [Header("Placed Objects")]
    [SerializeField] private ObjectsDatabaseSO database;
    [SerializeField] private Transform placedRoot;
    [SerializeField] private BoyEditMover boyMover;
    [Header("Debug")]
    [SerializeField] private bool debugLogs = true;

    private Mode mode;

    private InputActionMap buildMap;
    private InputActionMap gameModeMap;
    private InputAction toggleAction;

    public Mode CurrentMode => mode;
    public bool IsPlayMode => mode == Mode.Play;
    public bool IsEditMode => mode == Mode.Edit;

    private void Awake()
    {
        if (inputActions != null)
        {
            buildMap = inputActions.FindActionMap(buildMapName, true);
            gameModeMap = inputActions.FindActionMap(gameModeName, true);
            toggleAction = gameModeMap.FindAction(toggleActionName, true);
        }

        if (mainCamera == null)
            mainCamera = Camera.main;

        if (runtimeCoordinator == null)
            runtimeCoordinator = FindFirstObjectByType<LevelRuntimeCoordinator>();
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
        if (runtimeCoordinator == null)
        {
            Debug.LogWarning("[GameModeManager] runtimeCoordinator is null. Fallback toggle.");
            SetMode(mode == Mode.Play ? Mode.Edit : Mode.Play);
            return;
        }

        if (mode == Mode.Play)
            runtimeCoordinator.ReturnToEditFromPlay();
        else
            runtimeCoordinator.TryEnterPlay();
    }

    private void SetMode(Mode newMode)
    {
        mode = newMode;
        bool isPlay = (mode == Mode.Play);

        if (robot != null)
        {
            robot.SetInputEnabled(isPlay);

            if (!isPlay)
                robot.StopImmediately();
        }
        if (boyMover != null)
        {
            boyMover.SetGameModeLookTarget(robot != null ? robot.transform : null);
            boyMover.SetGameModeLookEnabled(isPlay);
        }
        ApplyPlacedColliderMode(isPlay);

        if (cameraOrbit != null)
        {
            if (disableCameraLookInEdit)
                cameraOrbit.SetInputEnabled(isPlay);
            else
                cameraOrbit.SetInputEnabled(true);
        }

        if (buildController != null)
            buildController.enabled = !isPlay;

        if (mainCamera != null)
            mainCamera.gameObject.SetActive(isPlay);

        if (editCamera != null)
            editCamera.gameObject.SetActive(!isPlay);

        Cursor.visible = !isPlay;
        Cursor.lockState = isPlay ? CursorLockMode.Locked : CursorLockMode.None;
        if (buildController is BuildController bc)
        {
            bc.CancelCurrentOperation(); // clears preview + drone
        }
        if (normalUIRoot != null)
            normalUIRoot.SetActive(!isPlay);

        if (backButton != null)
            backButton.SetActive(true);
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

        foreach (var bi in blocks)
        {
            if (bi == null) continue;

            if (debugLogs)
                Debug.Log($"[GameModeManager] ApplyColliderMode: {bi.name}, isPlay={isPlay}");

            bi.ApplyColliderMode(isPlay, database);
        }
    }
}