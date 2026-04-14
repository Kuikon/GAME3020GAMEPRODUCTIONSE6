using UnityEngine;


public class GoalTrigger : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private LevelRuntimeCoordinator runtimeCoordinator;

    [Header("Player")]
    [SerializeField] private string playerTag = "Player";

    [Header("Debug")]
    [SerializeField] private bool debugLogs = true;

    private void Reset()
    {
        Collider col = GetComponent<Collider>();
        if (col != null)
            col.isTrigger = true;
    }

    private void Awake()
    {
        if (runtimeCoordinator == null)
            runtimeCoordinator = FindFirstObjectByType<LevelRuntimeCoordinator>();
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision == null)
            return;

        if (!collision.gameObject.CompareTag(playerTag))
            return;

        if (debugLogs)
            Debug.Log($"[GoalTrigger] Player hit goal: {name}");

        runtimeCoordinator?.HandleGoalReached();
    }
}