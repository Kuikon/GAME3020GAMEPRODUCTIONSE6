using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class HammerTrap : MonoBehaviour
{
    [Header("Trigger")]
    [SerializeField] private Collider triggerCollider;
    [SerializeField] private string playerTag = "Player";

    [Header("Ground")]
    [SerializeField] private LayerMask groundMask;

    [Header("Shake")]
    [SerializeField] private float shakeDuration = 1.0f;
    [SerializeField] private float shakeAngle = 12f;
    [SerializeField] private float shakeSpeed = 18f;

    [Header("Drop")]
    [SerializeField] private float waitAfterHitGround = 1.0f; // ★ 地面に当たってから戻るまで
    [SerializeField] private float resetMoveSpeed = 6f;       // ★ 戻る速さ

    private Rigidbody rb;

    private bool activated;
    private bool dropping;
    private bool resetting;

    private Vector3 startPos;
    private Quaternion startRot;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();

        // 最初は固定
        rb.useGravity = false;
        rb.isKinematic = true;

        startPos = transform.position;
        startRot = transform.rotation;

        if (triggerCollider == null)
        {
            triggerCollider = GetComponent<Collider>();
            if (triggerCollider == null || !triggerCollider.isTrigger)
                triggerCollider = GetComponentInChildren<Collider>();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (activated || dropping || resetting) return;
        if (!other.CompareTag(playerTag)) return;

        activated = true;
        StartCoroutine(ShakeThenDrop());
    }

    // -----------------------------
    // 1) 揺れる → 落ちる
    // -----------------------------
    private IEnumerator ShakeThenDrop()
    {
        float t = 0f;

        while (t < shakeDuration)
        {
            t += Time.deltaTime;

            float angle = Mathf.Sin(t * shakeSpeed) * shakeAngle;
            transform.rotation = startRot * Quaternion.Euler(0f, 0f, angle);

            yield return null;
        }

        transform.rotation = startRot;

        // 落下開始
        dropping = true;
        rb.isKinematic = false;
        rb.useGravity = true;
    }

    // -----------------------------
    // 2) 地面に当たる
    // -----------------------------
    private void OnCollisionEnter(Collision collision)
    {
        if (!dropping) return;

        int otherLayer = 1 << collision.gameObject.layer;
        if ((groundMask.value & otherLayer) == 0) return;

        // 落下停止
        dropping = false;

        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        rb.useGravity = false;
        rb.isKinematic = true;

        StartCoroutine(ResetAfterDelay());
    }

    // -----------------------------
    // 3) 1秒後に元の位置へ戻る
    // -----------------------------
    private IEnumerator ResetAfterDelay()
    {
        resetting = true;

        yield return new WaitForSeconds(waitAfterHitGround);

        while (Vector3.Distance(transform.position, startPos) > 0.01f)
        {
            transform.position = Vector3.MoveTowards(
                transform.position,
                startPos,
                resetMoveSpeed * Time.deltaTime
            );

            transform.rotation = Quaternion.RotateTowards(
                transform.rotation,
                startRot,
                resetMoveSpeed * 100f * Time.deltaTime
            );

            yield return null;
        }

        // 完全リセット
        transform.position = startPos;
        transform.rotation = startRot;

        activated = false;
        resetting = false;
    }
}
