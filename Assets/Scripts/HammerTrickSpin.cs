using UnityEngine;

[RequireComponent(typeof(Rigidbody), typeof(HingeJoint))]
public class HammerTrickSpin : MonoBehaviour
{
    [Header("Swing")]
    [SerializeField] private float forwardSpeed = 260f;
    [SerializeField] private float backSpeed = 120f;
    [SerializeField] private float motorForce = 1200f;
    [SerializeField] private float edge = 0.25f;

    [Header("Direction")]
    [SerializeField] private bool reverseRotation = false;

    [Header("Hit Player")]
    [SerializeField] private float hitForce = 18f;
    [SerializeField] private float upForce = 6f;

    private HingeJoint hinge;

    // 論理的な往復方向
    private int dir = 1;

    void Awake()
    {
        hinge = GetComponent<HingeJoint>();
        hinge.useMotor = true;
        hinge.useLimits = true;
    }

    void FixedUpdate()
    {
        float ang = hinge.angle;
        float max = hinge.limits.max;
        float min = hinge.limits.min;

        int sign = reverseRotation ? -1 : 1;
        int actualDir = dir * sign; // ★ 実際に回っている方向

        // ★ actualDir で端判定（ここが超重要）
        if (actualDir > 0 && ang >= max - edge)
        {
            dir = -dir;   // ★ 強制反転
        }
        else if (actualDir < 0 && ang <= min + edge)
        {
            dir = -dir;   // ★ 強制反転
        }

        float speed = (dir > 0) ? forwardSpeed : backSpeed;

        JointMotor m = hinge.motor;
        m.force = motorForce;
        m.freeSpin = false;
        m.targetVelocity = speed * actualDir;
        hinge.motor = m;
    
    }

    void OnCollisionEnter(Collision collision)
    {
        if (!collision.collider.CompareTag("Player")) return;

        Rigidbody playerRb = collision.collider.attachedRigidbody;
        if (playerRb == null) return;

        Vector3 hitDir = (collision.transform.position - transform.position).normalized;
        hitDir.y = 0f;
        Debug.Log("Hit");
        Vector3 force = hitDir * hitForce + Vector3.up * upForce;
        playerRb.AddForce(force, ForceMode.Impulse);
    }
}
