using UnityEngine;
using UnityEngine.InputSystem;

public class GameModeManager : MonoBehaviour
{
    public enum Mode { Play, Edit }

    [Header("Input")]
    [SerializeField] private InputActionAsset inputActions; // same asset used by RobotController
    [SerializeField] private string buildMapName = "Build";
    [SerializeField] private string toggleActionName = "ToggleMode"; // Build/ToggleMode

    [Header("Refs")]
    [SerializeField] private RobotController robot;
    [SerializeField] private OrbitCamera cameraOrbit;

    [Header("Build Scripts (enable in Edit)")]
    [SerializeField] private MonoBehaviour buildController;   // BuildControllerNewInput
    [SerializeField] private MonoBehaviour cellHighlighter;   // CellHighlighterNewInput

  

    [Tooltip("Edit用の普通のCamera(=GameObjectをON/OFFする)")]
    [SerializeField] private Camera editCamera;

    [Tooltip("EditCameraについてるFreeFlyスクリプト(任意だけど入れた方が安全)")]
    [SerializeField] private FreeFlyCamera editFly;

    [Header("Options")]
    [SerializeField] private Mode startMode = Mode.Edit;
    [SerializeField] private bool disableCameraLookInEdit = true;

    [Header("Cinemachine Priority")]
    [SerializeField] private int playPriority = 20;
    [SerializeField] private int editPriority = 0;

    private Mode mode;

    private InputActionMap buildMap;
    private InputAction toggleAction;

    private void Awake()
    {
        buildMap = inputActions.FindActionMap(buildMapName, true);
        toggleAction = buildMap.FindAction(toggleActionName, true);
    }

    private void OnEnable()
    {
        buildMap.Enable();
        toggleAction.performed += OnToggle;
    }

    private void OnDisable()
    {
        toggleAction.performed -= OnToggle;
        buildMap.Disable();
    }

    private void Start()
    {
        SetMode(startMode);
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
            if (!isPlay) robot.StopImmediately();
        }

        // =========================
        // Camera look (Play用Orbit等)
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
        if (buildController != null) buildController.enabled = !isPlay;
        if (cellHighlighter != null) cellHighlighter.enabled = !isPlay;

        // =========================
        // Camera switching
        // =========================


        // Edit: editCameraをON（普通のCamera）
        if (editCamera != null)
            editCamera.gameObject.SetActive(!isPlay);

        // Edit: フリーフライ入力
        if (editFly != null)
            editFly.SetInputEnabled(!isPlay);

        // =========================
        // Cursor
        // =========================
        Cursor.visible = !isPlay;
        Cursor.lockState = isPlay ? CursorLockMode.Locked : CursorLockMode.None;

        Debug.Log($"Mode: {mode}");
    }
}
