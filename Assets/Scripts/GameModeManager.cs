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
    [SerializeField] private RobotControllerCommander robot;
    [SerializeField] private OrbitCamera cameraOrbit;

    [Header("Build Scripts (enable in Edit)")]
    [SerializeField] private MonoBehaviour buildController;   // BuildControllerNewInput

    [SerializeField] private Camera editCamera;

    [SerializeField] private FreeFlyCamera editFly;
    [SerializeField] private Mode startMode = Mode.Edit;
    [SerializeField] private bool disableCameraLookInEdit = true;
    [SerializeField] private ObjectsDatabaseSO database; // ★ 追加
    [SerializeField] private Transform placedRoot;
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
    public void ForceModeEdit() => SetMode(Mode.Edit);
    public void ForceModePlay() => SetMode(Mode.Play);
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
        ApplyPlacedColliderMode(isPlay);
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
    private void ApplyPlacedColliderMode(bool isPlay)
    {
        if (database == null) return;

        // 置いた物だけに絞る（placedRootがあるならそれが一番安全＆軽い）
        if (placedRoot != null)
        {
            var blocks = placedRoot.GetComponentsInChildren<BlockInstance>(true);
            foreach (var bi in blocks)
                if (bi) bi.ApplyColliderMode(isPlay, database);
            Debug.Log($"[ColliderSwitch] Found blocks: {blocks.Length}");

            foreach (var bi in blocks)
            {
                Debug.Log($"[ColliderSwitch] Applying to {bi.name}");
                bi.ApplyColliderMode(isPlay, database);
            }
            return;
        }
    

      
    }
}
