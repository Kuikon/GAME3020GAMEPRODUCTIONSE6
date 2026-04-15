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
    [SerializeField] private float beforeMoveToVictoryCameraDelay = 0.5f;
    [SerializeField] private float beforeVictoryDelay = 0.2f;
    [SerializeField] private float beforeReturnToEditDelay = 1.2f;

    [Header("Wave Camera")]
    [SerializeField] private float waveCameraDistance = 1.8f;
    [SerializeField] private float waveCameraHeight = 1.4f;
    [SerializeField] private Vector3 waveLookOffset = new Vector3(0f, 1.2f, 0f);
    [SerializeField] private float robotFaceCameraEyeHeight = 1.2f;
    [SerializeField] private bool snapWaveCameraImmediately = true;

    [Header("Victory Camera")]
    [SerializeField] private Transform boyVictoryCameraPoint;
    [SerializeField] private Vector3 boyVictoryLookOffset = new Vector3(0f, 1.2f, 0f);
    [SerializeField] private bool snapVictoryCameraImmediately = true;

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

        if (!MovePlayerToStart())
            return false;

        modeManager?.ForceModePlay();
        return true;
    }

    public void ReturnToEditFromPlay()
    {
        if (orbitCamera != null)
        {
            orbitCamera.ClearGoalShot();
            orbitCamera.SetInputEnabled(true);
        }

        MovePlayerToStart();

        if (boyMover != null)
        {
            boyMover.SetInputEnabled(true);
        }

        modeManager?.ForceModeEdit();
    }

    public void HandleGoalReached()
    {
        if (goalSequenceRunning)
            return;

        goalSequenceRunning = true;

        if (debugLogs)
            Debug.Log("[LevelRuntimeCoordinator] Goal reached. Start goal sequence.");

        StartCoroutine(CoHandleGoalReached());
    }

    private IEnumerator CoHandleGoalReached()
    {
        // 1. 止める
        if (robotController != null)
        {
            robotController.SetInputEnabled(false);
            robotController.StopImmediately();
        }

        if (boyMover != null)
        {
            boyMover.SetInputEnabled(false);
        }

        if (orbitCamera != null)
        {
            orbitCamera.SetInputEnabled(false);
        }

        // 2.camera move to robot
        Vector3 waveCamPos = Vector3.zero;

        if (orbitCamera != null && robotTransform != null)
        {
            waveCamPos =
                robotTransform.position
                + robotTransform.forward * waveCameraDistance
                + Vector3.up * waveCameraHeight;

            Vector3 waveLookTarget = robotTransform.position + waveLookOffset;

            orbitCamera.MoveToGoalShot(waveCamPos, waveLookTarget, snapWaveCameraImmediately);

            if (debugLogs)
                Debug.Log("[LevelRuntimeCoordinator] Wave camera shot activated.");
        }
        // 3.Robot rotate to camera
        if (robotController != null && robotTransform != null)
        {
            Vector3 robotLookAt = waveCamPos;
            robotLookAt.y = robotTransform.position.y + robotFaceCameraEyeHeight;

            robotController.FaceWorldPositionInstant(robotLookAt);

            if (debugLogs)
                Debug.Log("[LevelRuntimeCoordinator] Robot turned toward camera.");
        }

        // 4. Wait
        if (beforeWaveDelay > 0f)
            yield return new WaitForSeconds(beforeWaveDelay);

        // 5. Robot wave
        if (robotAnimator != null && !string.IsNullOrEmpty(robotWaveTrigger))
        {
            robotAnimator.ResetTrigger(robotWaveTrigger);
            robotAnimator.SetTrigger(robotWaveTrigger);

            if (debugLogs)
                Debug.Log("[LevelRuntimeCoordinator] Robot plays Wave.");
        }

        // 6. Wait
        if (beforeMoveToVictoryCameraDelay > 0f)
            yield return new WaitForSeconds(beforeMoveToVictoryCameraDelay);

        // 7. move to fixed camera position
        if (orbitCamera != null && boyVictoryCameraPoint != null && playerTransform != null)
        {
            Vector3 camPos = boyVictoryCameraPoint.position;
            Vector3 lookTarget = playerTransform.position + boyVictoryLookOffset;

            orbitCamera.MoveToGoalShot(camPos, lookTarget, snapVictoryCameraImmediately);

            if (debugLogs)
                Debug.Log("[LevelRuntimeCoordinator] Camera moved to boy victory point.");
        }

        // 8. wait
        if (beforeVictoryDelay > 0f)
            yield return new WaitForSeconds(beforeVictoryDelay);

        // 9. boy victory
        if (boyAnimator != null && !string.IsNullOrEmpty(boyVictoryTrigger))
        {
            boyAnimator.ResetTrigger(boyVictoryTrigger);
            boyAnimator.SetTrigger(boyVictoryTrigger);

            if (debugLogs)
                Debug.Log("[LevelRuntimeCoordinator] Boy plays Victory.");
        }

        // 10. wait
        if (beforeReturnToEditDelay > 0f)
            yield return new WaitForSeconds(beforeReturnToEditDelay);

        // 11.Go back to Edit 
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
}