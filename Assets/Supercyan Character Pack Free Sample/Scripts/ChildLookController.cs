using UnityEngine;

public class ChildLookController : MonoBehaviour
{
    [Header("Look")]
    [SerializeField] private Transform lookTarget;   // プレイヤー
    [SerializeField] private float rotateSpeed = 5f;

    [Header("Patrol")]
    [SerializeField] private Transform[] patrolPoints; // ★ テーブル角
    [SerializeField] private float moveSpeed = 1.5f;
    [SerializeField] private float arriveDistance = 0.1f;

    private int currentIndex = 0;

    void Update()
    {
        PatrolMove();
        LookAtPlayer();
    }

    // -----------------------------
    // パトロール移動
    // -----------------------------
    void PatrolMove()
    {
        if (patrolPoints == null || patrolPoints.Length == 0) return;

        Transform targetPoint = patrolPoints[currentIndex];

        Vector3 targetPos = targetPoint.position;
        targetPos.y = transform.position.y; // 高さ固定

        // 移動
        transform.position = Vector3.MoveTowards(
            transform.position,
            targetPos,
            moveSpeed * Time.deltaTime
        );

        // 到着判定
        if (Vector3.Distance(transform.position, targetPos) < arriveDistance)
        {
            currentIndex = (currentIndex + 1) % patrolPoints.Length;
        }
    }

    // -----------------------------
    // プレイヤーを見る（体ごと）
    // -----------------------------
    void LookAtPlayer()
    {
        if (lookTarget == null) return;

        Vector3 dir = lookTarget.position - transform.position;
        dir.y = 0f;

        if (dir.sqrMagnitude < 0.0001f) return;

        Quaternion targetRotation = Quaternion.LookRotation(dir);

        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            targetRotation,
            rotateSpeed * Time.deltaTime
        );
    }
}
