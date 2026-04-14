using System.Collections;
using UnityEngine;

public class LevelRuntimeCoordinator : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private ObjectsDatabaseSO database;
    [SerializeField] private GameModeManager modeManager;
    [SerializeField] private Transform playerTransform;

    [Header("Goal Sequence Refs")]
    [SerializeField] private Transform robotTransform;
    [SerializeField] private BoyEditMover boyMover;
    [SerializeField] private RobotControllerCommander robotController;
    [SerializeField] private Animator robotAnimator;
    [SerializeField] private Animator boyAnimator;
    [SerializeField] private OrbitCamera orbitCamera;

    [Header("Spawn")]
    [SerializeField] private float spawnPadding = 0.05f;

    [Header("Goal Animation")]
    [SerializeField] private string robotWaveTrigger = "Wave";
    [SerializeField] private string boyVictoryTrigger = "Victory";
    [SerializeField] private float beforeWaveDelay = 0.1f;
    [SerializeField] private float beforeVictoryDelay = 0.35f;
    [SerializeField] private float beforeReturnToEditDelay = 1.2f;
    [SerializeField] private bool makeRobotFaceBoy = true;
    [SerializeField] private bool makeBoyFaceRobot = true;

    [Header("Goal Camera")]
    [SerializeField] private bool useGoalCameraShot = true;
    [SerializeField] private float cameraBehindRobotDistance = 2.5f;
    [SerializeField] private float cameraHeightOffset = 1.25f;
    [SerializeField] private float cameraLookAtHeight = 1.0f;
    [SerializeField] private bool snapGoalCameraImmediately = true;

    [Header("Debug")]
    [SerializeField] private bool debugLogs = true;

    private LevelRuleService ruleService;
    private bool goalSequenceRunning;

    private void Awake()
    {
        ruleService = new LevelRuleService(database);

        if (modeManager == null)
            modeManager = FindFirstObjectByType<GameModeManager>();

        if (boyMover == null && playerTransform != null)
            boyMover = playerTransform.GetComponent<BoyEditMover>();

        if (boyAnimator == null && playerTransform != null)
            boyAnimator = playerTransform.GetComponentInChildren<Animator>();

        if (robotAnimator == null && robotTransform != null)
            robotAnimator = robotTransform.GetComponentInChildren<Animator>();

        if (robotController == null && robotTransform != null)
            robotController = robotTransform.GetComponent<RobotControllerCommander>();

        if (orbitCamera == null)
            orbitCamera = FindFirstObjectByType<OrbitCamera>();
    }

    public bool TryEnterPlay()
    {
        if (ruleService == null)
        {
            Debug.LogWarning("[LevelRuntimeCoordinator] ruleService is null.");
            return false;
        }

        if (!ruleService.HasStartBlock())
        {
            Debug.LogWarning("[LevelRuntimeCoordinator] Startブロックがありません。");
            return false;
        }

        if (!ruleService.HasGoalBlock())
        {
            Debug.LogWarning("[LevelRuntimeCoordinator] Goalブロックがありません。");
            return false;
        }

        goalSequenceRunning = false;

        if (orbitCamera != null)
        {
            orbitCamera.ClearCinematicOverride();
        }

        if (!MovePlayerToStart())
            return false;

        modeManager?.ForceModePlay();
        return true;
    }

    public void ReturnToEditFromPlay()
    {
        if (orbitCamera != null)
        {
            orbitCamera.ClearCinematicOverride();
        }

        MovePlayerToStart();
        modeManager?.ForceModeEdit();
    }

    public void HandleGoalReached()
    {
        if (goalSequenceRunning)
            return;

        goalSequenceRunning = true;
        StartCoroutine(CoHandleGoalReached());
    }

    private IEnumerator CoHandleGoalReached()
    {
        if (robotController != null)
        {
            robotController.SetInputEnabled(false);
            robotController.StopImmediately();
        }

        if (boyMover != null)
        {
            boyMover.SetInputEnabled(false);
        }

        if (makeRobotFaceBoy && robotController != null && playerTransform != null)
        {
            robotController.FaceTargetInstant(playerTransform);
        }

        if (makeBoyFaceRobot && playerTransform != null && robotTransform != null)
        {
            FaceTargetFlat(playerTransform, robotTransform.position);

            if (boyMover != null)
                boyMover.SetFacingYaw(playerTransform.eulerAngles.y);
        }

        if (useGoalCameraShot && orbitCamera != null && robotTransform != null && playerTransform != null)
        {
            Vector3 camPos;
            Quaternion camRot;
            BuildGoalCameraPose(robotTransform, playerTransform, out camPos, out camRot);

            orbitCamera.SetInputEnabled(false);
            orbitCamera.EnableCinematicOverride(true);
            orbitCamera.SetCinematicPose(camPos, camRot, snapGoalCameraImmediately);
        }

        if (beforeWaveDelay > 0f)
            yield return new WaitForSeconds(beforeWaveDelay);

        if (robotAnimator != null && !string.IsNullOrEmpty(robotWaveTrigger))
        {
            robotAnimator.ResetTrigger(robotWaveTrigger);
            robotAnimator.SetTrigger(robotWaveTrigger);
        }

        if (beforeVictoryDelay > 0f)
            yield return new WaitForSeconds(beforeVictoryDelay);

        if (boyAnimator != null && !string.IsNullOrEmpty(boyVictoryTrigger))
        {
            boyAnimator.ResetTrigger(boyVictoryTrigger);
            boyAnimator.SetTrigger(boyVictoryTrigger);
        }

        if (beforeReturnToEditDelay > 0f)
            yield return new WaitForSeconds(beforeReturnToEditDelay);

        ReturnToEditFromPlay();
        goalSequenceRunning = false;
    }

    public bool MovePlayerToStart()
    {
        if (ruleService == null)
        {
            Debug.LogWarning("[LevelRuntimeCoordinator] ruleService is null.");
            return false;
        }

        if (playerTransform == null)
        {
            Debug.LogWarning("[LevelRuntimeCoordinator] playerTransform is null.");
            return false;
        }

        BlockInstance startBlock = ruleService.GetStartBlock();
        if (startBlock == null)
        {
            Debug.LogWarning("[LevelRuntimeCoordinator] Startブロックがありません。");
            return false;
        }

        Vector3 spawnPosition = CalculateSafeSpawnPosition(startBlock, playerTransform);

        Rigidbody rb = playerTransform.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        CharacterController cc = playerTransform.GetComponent<CharacterController>();
        if (cc != null)
            cc.enabled = false;

        playerTransform.position = spawnPosition;

        if (cc != null)
            cc.enabled = true;

        return true;
    }

    private Vector3 CalculateSafeSpawnPosition(BlockInstance startBlock, Transform player)
    {
        float startTopY = startBlock.transform.position.y;

        Collider startCol = startBlock.GetComponentInChildren<Collider>();
        if (startCol != null)
            startTopY = startCol.bounds.max.y;

        float playerHalfHeight = 1f;

        CapsuleCollider capsule = player.GetComponent<CapsuleCollider>();
        if (capsule != null)
        {
            playerHalfHeight = capsule.bounds.extents.y;
        }
        else
        {
            CharacterController cc = player.GetComponent<CharacterController>();
            if (cc != null)
                playerHalfHeight = cc.bounds.extents.y;
        }

        return new Vector3(
            startBlock.transform.position.x,
            startTopY + playerHalfHeight + spawnPadding,
            startBlock.transform.position.z
        );
    }

    private void FaceTargetFlat(Transform actor, Vector3 targetPosition)
    {
        if (actor == null)
            return;

        Vector3 toTarget = targetPosition - actor.position;
        toTarget.y = 0f;

        if (toTarget.sqrMagnitude < 0.0001f)
            return;

        actor.rotation = Quaternion.LookRotation(toTarget.normalized, Vector3.up);
    }

    private void BuildGoalCameraPose(Transform robot, Transform boy, out Vector3 cameraPosition, out Quaternion cameraRotation)
    {
        Vector3 robotPos = robot.position;
        Vector3 boyPos = boy.position;

        Vector3 robotForward = robot.forward;
        robotForward.y = 0f;

        if (robotForward.sqrMagnitude < 0.0001f)
            robotForward = (boyPos - robotPos).normalized;

        robotForward.Normalize();

        cameraPosition =
            robotPos
            - robotForward * cameraBehindRobotDistance
            + Vector3.up * cameraHeightOffset;

        Vector3 lookTarget = boyPos + Vector3.up * cameraLookAtHeight;
        Vector3 lookDir = lookTarget - cameraPosition;

        if (lookDir.sqrMagnitude < 0.0001f)
            lookDir = robotForward;

        cameraRotation = Quaternion.LookRotation(lookDir.normalized, Vector3.up);
    }
}