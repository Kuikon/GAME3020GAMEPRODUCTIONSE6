using UnityEngine;

public class LevelRuntimeCoordinator : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private ObjectsDatabaseSO database;
    [SerializeField] private GameModeManager modeManager;
    [SerializeField] private Transform playerTransform;

    [Header("Spawn")]
    [SerializeField] private float spawnPadding = 0.05f;

    [Header("Debug")]
    [SerializeField] private bool debugLogs = true;

    private LevelRuleService ruleService;

    private void Awake()
    {
        ruleService = new LevelRuleService(database);

        if (modeManager == null)
            modeManager = FindFirstObjectByType<GameModeManager>();
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

        if (!MovePlayerToStart())
            return false;

        if (debugLogs)
            Debug.Log("[LevelRuntimeCoordinator] TryEnterPlay -> ForceModePlay");

        modeManager?.ForceModePlay();
        return true;
    }

    public void ReturnToEditFromPlay()
    {
        MovePlayerToStart();

        if (debugLogs)
            Debug.Log("[LevelRuntimeCoordinator] ReturnToEditFromPlay -> ForceModeEdit");

        modeManager?.ForceModeEdit();
    }

    public void HandleGoalReached()
    {
        if (debugLogs)
            Debug.Log("[LevelRuntimeCoordinator] Goal reached.");

        ReturnToEditFromPlay();
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

        if (debugLogs)
            Debug.Log($"[LevelRuntimeCoordinator] MovePlayerToStart = {spawnPosition}");

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