using System.Collections;
using UnityEngine;

public class SpringPad : MonoBehaviour
{
    [Header("Tip Trigger")]
    [SerializeField] private Collider tipTrigger;
    [SerializeField] private Transform tipNormalRoot; // 先端の向き（法線）

    [Header("Visual")]
    [SerializeField] private float compressScaleY = 0.5f;
    [SerializeField] private float compressTime = 0.08f;
    [SerializeField] private float returnTime = 0.12f;

    [Header("Reflection")]
    [SerializeField] private float reflectPower = 12f;     // 反射の強さ
    [SerializeField] private float impulseLockTime = 0.12f; // 操作ロック時間（短くてOK）

    [Header("Detection")]
    [SerializeField] private string playerTag = "Player";

    private Vector3 startScale;
    private bool isAnimating;

    private void Awake()
    {
        startScale = transform.localScale;
        if (tipNormalRoot == null && tipTrigger != null)
            tipNormalRoot = tipTrigger.transform;
    }

    // ⭐ Trigger
    private void OnTriggerEnter(Collider other)
    {
        if (isAnimating) return;
        if (!other.CompareTag(playerTag)) return;

        Rigidbody rb = other.attachedRigidbody;
        if (rb == null) return;

        Vector3 incomingVelocity = rb.linearVelocity;
        if (incomingVelocity.sqrMagnitude < 0.0001f) return;

        // 先端の法線（ワールド）
        Vector3 hitNormal = tipNormalRoot.up.normalized;

        // 完全反射方向
        Vector3 reflectDir = Vector3.Reflect(incomingVelocity.normalized, hitNormal);

        StartCoroutine(SpringRoutine(rb, reflectDir));
    }
    private void OnTriggerStay(Collider other)
    {
        OnTriggerEnter(other);
    }
    private IEnumerator SpringRoutine(Rigidbody rb, Vector3 reflectDir)
    {
        isAnimating = true;

        // ===== PlayerController 対策 =====
        // 一瞬だけ drag を下げて「力が消されにくく」する
        float savedDrag = rb.linearDamping;
        rb.linearDamping = 0f;

        // 見た目：圧縮
        Vector3 compressed = new Vector3(
            startScale.x,
            startScale.y * compressScaleY,
            startScale.z
        );

        float t = 0f;
        while (t < compressTime)
        {
            t += Time.deltaTime;
            transform.localScale = Vector3.Lerp(startScale, compressed, t / compressTime);
            yield return null;
        }
        transform.localScale = compressed;

        // ⭐ 物理フレーム後に力を入れる（超重要）
        yield return new WaitForFixedUpdate();
        RobotController rc = rb.GetComponent<RobotController>();
        if (rc != null)
        {
            rc.ApplyExternalImpulse();
        }
        // 既存の速度を一度消してから反射力を入れる
        rb.linearVelocity = Vector3.zero;
        rb.AddForce(reflectDir * reflectPower, ForceMode.Impulse);

        // 操作ロック時間
        yield return new WaitForSeconds(impulseLockTime);

        // drag を元に戻す
        rb.linearDamping = savedDrag;

        // 見た目：戻す
        t = 0f;
        while (t < returnTime)
        {
            t += Time.deltaTime;
            transform.localScale = Vector3.Lerp(compressed, startScale, t / returnTime);
            yield return null;
        }
        transform.localScale = startScale;

        isAnimating = false;
    }
}
