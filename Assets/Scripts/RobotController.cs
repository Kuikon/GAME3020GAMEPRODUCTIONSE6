using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(CapsuleCollider))]
public class RobotController : MonoBehaviour
{
    // =========================================================
    // Inspector
    // =========================================================

    [Header("Camera")]
    [SerializeField] private Transform cameraTransform;

    [Header("Input")]
    [SerializeField] private InputActionAsset inputActions;

    [Header("Movement")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float rotateSpeed = 12f;

    [Header("Jump")]
    [SerializeField] private float jumpForce = 7f;
    [SerializeField] private bool canDoubleJump = true;

    [Header("Jump Assist")]
    [SerializeField] private float jumpUngroundTime = 0.1f; // ★重要

    [Header("Animation")]
    [SerializeField] private Animator animator;

    [Header("Conveyor")]
    [SerializeField] private float conveyorStickTime = 0.1f;

    [Header("Impulse Stop Layers")]
    [SerializeField] private LayerMask impulseStopLayers;

    [Header("Ground")]
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private float groundSnapDistance = 0.5f;
    [SerializeField] private float groundSkin = 0.05f;
    [SerializeField] private float maxSlopeAngle = 60f;

    // =========================================================
    // Components
    // =========================================================

    private Rigidbody rb;
    private CapsuleCollider capsule;

    // =========================================================
    // Input
    // =========================================================

    private InputActionMap playerMap;
    private InputAction moveAction;
    private InputAction jumpAction;

    // =========================================================
    // State
    // =========================================================

    public bool isGrounded { get; private set; }
    private bool usedDoubleJump;

    private bool isWallContact;
    private Vector3 wallNormal;

    private Vector3 conveyorVelocity;
    private float conveyorTimer;

    private bool isExternalImpulseActive;
    private RaycastHit groundHit;

    private float ungroundTimer;

    // =========================================================
    // Unity Lifecycle
    // =========================================================

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        capsule = GetComponent<CapsuleCollider>();
        rb.freezeRotation = true;

        playerMap = inputActions.FindActionMap("Player", true);
        moveAction = playerMap.FindAction("Move", true);
        jumpAction = playerMap.FindAction("Jump", true);
    }

    private void OnEnable()
    {
        playerMap.Enable();
        jumpAction.performed += OnJump;
    }

    private void OnDisable()
    {
        jumpAction.performed -= OnJump;
        playerMap.Disable();
    }

    private void Update()
    {
        UpdateAnimator();
    }

    private void FixedUpdate()
    {
        if (isExternalImpulseActive)
            return;

        // ★ジャンプ直後の接地無効
        if (ungroundTimer > 0f)
            ungroundTimer -= Time.fixedDeltaTime;

        if (ungroundTimer <= 0f)
            UpdateGround();
        else
            isGrounded = false;

        UpdateConveyor();
        HandleMovement();
        HandleRotation();
    }

    // =========================================================
    // Movement
    // =========================================================

    private void HandleMovement()
    {
        Vector2 input = moveAction.ReadValue<Vector2>();
        Vector3 moveDir = GetCameraRelativeDirection(input);
        Vector3 velocity = rb.linearVelocity;

        Vector3 horizontalVelocity = moveDir * moveSpeed;

        if (isGrounded)
        {
            velocity.y = Mathf.Min(velocity.y, 0f);

            if (input.sqrMagnitude < 0.01f)
            {
                rb.linearVelocity = new Vector3(
                    conveyorVelocity.x,
                    velocity.y,
                    conveyorVelocity.z
                );
                return;
            }
        }

        Vector3 desiredVelocity = new Vector3(
            horizontalVelocity.x,
            velocity.y,
            horizontalVelocity.z
        );

        desiredVelocity = ApplyWallConstraint(desiredVelocity);
        desiredVelocity += conveyorVelocity;

        rb.linearVelocity = desiredVelocity;
    }

    private void HandleRotation()
    {
        Vector2 input = moveAction.ReadValue<Vector2>();
        if (input.sqrMagnitude < 0.01f) return;

        Vector3 moveDir = GetCameraRelativeDirection(input);
        if (moveDir.sqrMagnitude < 0.001f) return;

        Quaternion target = Quaternion.LookRotation(moveDir, Vector3.up);
        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            target,
            rotateSpeed * Time.fixedDeltaTime
        );
    }

    private Vector3 GetCameraRelativeDirection(Vector2 input)
    {
        Vector3 forward = cameraTransform.forward;
        Vector3 right = cameraTransform.right;

        forward.y = 0f;
        right.y = 0f;

        return (forward.normalized * input.y + right.normalized * input.x).normalized;
    }

    private Vector3 ApplyWallConstraint(Vector3 velocity)
    {
        if (!isWallContact) return velocity;

        float intoWall = Vector3.Dot(velocity, wallNormal);
        if (intoWall < 0f)
            velocity -= wallNormal * intoWall;

        return velocity;
    }

    // =========================================================
    // Jump
    // =========================================================

    private void OnJump(InputAction.CallbackContext ctx)
    {
        if (isGrounded)
        {
            DoJump();
            usedDoubleJump = false;
        }
        else if (canDoubleJump && !usedDoubleJump)
        {
            DoJump();
            usedDoubleJump = true;
        }
    }

    private void DoJump()
    {
        Vector3 v = rb.linearVelocity;
        v.y = jumpForce;
        rb.linearVelocity = v;

        // ★最重要
        isGrounded = false;
        ungroundTimer = jumpUngroundTime;

        if (animator)
            animator.SetTrigger("Jump");
    }

    // =========================================================
    // Ground
    // =========================================================

    private void UpdateGround()
    {
        isGrounded = false;

        if (!Physics.CapsuleCast(
            GetCapsuleTop(),
            GetCapsuleBottom(),
            GetCapsuleRadius() - groundSkin,
            Vector3.down,
            out groundHit,
            groundSnapDistance,
            groundLayer,
            QueryTriggerInteraction.Ignore))
            return;

        float slopeAngle = Vector3.Angle(groundHit.normal, Vector3.up);
        isGrounded = slopeAngle <= maxSlopeAngle;

        if (isGrounded)
            usedDoubleJump = false;
    }

    // =========================================================
    // Conveyor
    // =========================================================

    private void UpdateConveyor()
    {
        if (conveyorTimer <= 0f) return;

        conveyorTimer -= Time.fixedDeltaTime;
        if (conveyorTimer <= 0f)
            conveyorVelocity = Vector3.zero;
    }

    // =========================================================
    // Capsule Helpers
    // =========================================================

    private float GetCapsuleRadius()
        => capsule.radius * Mathf.Max(transform.localScale.x, transform.localScale.z);

    private float GetCapsuleHalfHeight()
    {
        float height = Mathf.Max(
            capsule.height * transform.localScale.y,
            GetCapsuleRadius() * 2f
        );
        return (height * 0.5f) - GetCapsuleRadius();
    }

    private Vector3 GetCapsuleTop()
        => transform.TransformPoint(capsule.center + Vector3.up * GetCapsuleHalfHeight());

    private Vector3 GetCapsuleBottom()
        => transform.TransformPoint(capsule.center - Vector3.up * GetCapsuleHalfHeight());
    public void ApplyExternalImpulse()
    {
        isExternalImpulseActive = true;
    }
    // =========================================================
    // Collision
    // =========================================================
    private void OnCollisionEnter(Collision collision)
    {
        if (!isExternalImpulseActive)
            return;

        // 指定レイヤーに当たったら解除
        if (((1 << collision.gameObject.layer) & impulseStopLayers) != 0)
        {
            isExternalImpulseActive = false;
        }
    }
    private void OnCollisionStay(Collision collision)
    {
        isWallContact = false;

        foreach (var c in collision.contacts)
        {
            if (c.normal.y < 0.2f)
            {
                isWallContact = true;
                wallNormal = c.normal;
                break;
            }
        }
    }

    private void OnCollisionExit(Collision collision)
    {
        isWallContact = false;
    }

    // =========================================================
    // Public API
    // =========================================================

    public void SetConveyorVelocity(Vector3 velocity)
    {
        conveyorVelocity = velocity;
        conveyorTimer = conveyorStickTime;
    }

    public void SetInputEnabled(bool enabled)
    {
        if (enabled) playerMap.Enable();
        else playerMap.Disable();
    }
    public void StopImmediately()
    {
        conveyorVelocity = Vector3.zero;
        conveyorTimer = 0f;

        // 速度を止める
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        // 外部インパルス中なら解除（必要なら）
        isExternalImpulseActive = false;

        // 壁接触リセット
        isWallContact = false;
    }
    // =========================================================
    // Animation
    // =========================================================

    private void UpdateAnimator()
    {
        if (!animator) return;

        Vector3 v = rb.linearVelocity;
        animator.SetFloat("Speed", new Vector3(v.x, 0f, v.z).magnitude);
        animator.SetBool("IsGrounded", isGrounded);
    }
}
