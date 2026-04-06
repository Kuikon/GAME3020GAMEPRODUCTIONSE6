using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(CapsuleCollider))]
public class RobotControllerCommander : MonoBehaviour
{
    [Header("Camera")]
    public Transform cameraTransform;

    [Header("Input")]
    public InputActionAsset inputActions;

    [Header("Ground")]
    public LayerMask groundLayer;
    public float groundCheckDistance = 0.08f;
    public Transform groundPoint;
    public float groundCheckRadius = 0.2f;

    [Header("Movement")]
    public float moveSpeed = 5f;
    public float runSpeed = 8f;
    public float rotateSpeed = 12f;
    public float groundAccel = 35f;
    public float airAccel = 12f;

    [Header("Jump")]
    public float jumpHeight = 1.8f;
    public float jumpBufferTime = 0.12f;
    public float coyoteTime = 0.12f;
    public float jumpCutMultiplier = 0.5f;
    public float fallGravityMultiplier = 2.0f;
    public float lowJumpGravityMultiplier = 2.5f;

    [Header("Death")]
    public LayerMask deathLayer;
    public string deathTriggerName = "Die";
    [SerializeField] private float respawnDelay = 1.2f;
    private bool isDead;

    [Header("Animation")]
    public Animator animator;

    [Header("Conveyor")]
    public float conveyorStickTime = 0.1f;

    [Header("Respawn")]
    [SerializeField] private LevelRuntimeCoordinator runtimeCoordinator;

    private RobotContext ctx;

    private readonly RobotFactsBinder facts = new RobotFactsBinder();
    private readonly RobotStateLogic state = new RobotStateLogic();
    private readonly RobotTransformLogic xform = new RobotTransformLogic();
    private readonly RobotJudgmentLogic judge = new RobotJudgmentLogic();
    private readonly RobotOutputLogic output = new RobotOutputLogic();

    private System.Action<InputAction.CallbackContext> jumpPerformedHandler;
    private System.Action<InputAction.CallbackContext> jumpCanceledHandler;

    private Coroutine respawnRoutine;

    private void Awake()
    {
        ctx = new RobotContext();
        facts.Bind(ctx, this);

        jumpPerformedHandler = OnJumpPerformed;
        jumpCanceledHandler = OnJumpCanceled;

        if (runtimeCoordinator == null)
            runtimeCoordinator = FindFirstObjectByType<LevelRuntimeCoordinator>();
    }

    private void OnEnable()
    {
        if (ctx.PlayerMap != null)
            ctx.PlayerMap.Enable();

        if (ctx.JumpAction != null)
        {
            ctx.JumpAction.performed += jumpPerformedHandler;
            ctx.JumpAction.canceled += jumpCanceledHandler;
        }
    }

    private void OnDisable()
    {
        if (ctx.JumpAction != null)
        {
            ctx.JumpAction.performed -= jumpPerformedHandler;
            ctx.JumpAction.canceled -= jumpCanceledHandler;
        }

        if (ctx.PlayerMap != null)
            ctx.PlayerMap.Disable();
    }

    private void Update()
    {
        if (isDead)
            return;

        ctx.MoveInput = ctx.MoveAction.ReadValue<Vector2>();

        if (ctx.RunAction != null)
            ctx.RunHeld = ctx.RunAction.ReadValue<float>() > 0.5f;
        else
            ctx.RunHeld = false;
    }

    private void FixedUpdate()
    {
        if (isDead)
            return;

        ctx.Dt = Time.fixedDeltaTime;

        judge.Tick(ctx);
        state.Tick(ctx);
        xform.Tick(ctx);
        output.Tick(ctx);
    }

    private void OnJumpPerformed(InputAction.CallbackContext _)
    {
        if (isDead)
            return;

        ctx.JumpPressed = true;
        ctx.JumpHeld = true;
    }

    private void OnJumpCanceled(InputAction.CallbackContext _)
    {
        if (isDead)
            return;

        ctx.JumpHeld = false;
        ctx.JumpReleased = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (isDead)
            return;

        if (other.gameObject.CompareTag("Death"))
        {
            Die();
        }
    }

    private void Die()
    {
        if (isDead)
            return;

        isDead = true;

        ctx.MoveInput = Vector2.zero;
        ctx.RunHeld = false;
        ctx.JumpHeld = false;
        ctx.JumpPressed = false;
        ctx.JumpReleased = false;

        SetInputEnabled(false);
        StopImmediately();

        if (animator != null)
            animator.SetTrigger(deathTriggerName);

        if (respawnRoutine != null)
            StopCoroutine(respawnRoutine);

        respawnRoutine = StartCoroutine(CoRespawn());
    }

    private IEnumerator CoRespawn()
    {
        yield return new WaitForSeconds(respawnDelay);

        if (runtimeCoordinator != null)
        {
            runtimeCoordinator.MovePlayerToStart();
        }
        else
        {
            Debug.LogWarning("[RobotControllerCommander] LevelRuntimeCoordinator Ç™å©Ç¬Ç©ÇËÇ‹ÇπÇÒÅB");
        }

        ResetDeadState();

        respawnRoutine = null;
    }

    private void ResetDeadState()
    {
        isDead = false;

        ctx.MoveInput = Vector2.zero;
        ctx.RunHeld = false;
        ctx.JumpHeld = false;
        ctx.JumpPressed = false;
        ctx.JumpReleased = false;
        ctx.ConveyorVelocity = Vector3.zero;
        ctx.ConveyorTimer = 0f;

        StopImmediately();
        SetInputEnabled(true);

        if (animator != null)
        {
            animator.ResetTrigger(deathTriggerName);
            animator.Update(0f);
        }
    }

    private bool IsInLayerMask(int layer, LayerMask mask)
    {
        return (mask.value & (1 << layer)) != 0;
    }

    // ===== Public API =====
    public void SetConveyorVelocity(Vector3 velocity)
    {
        ctx.ConveyorVelocity = velocity;
        ctx.ConveyorTimer = ctx.ConveyorStickTime;
    }

    public void SetInputEnabled(bool enabled)
    {
        if (ctx.PlayerMap == null)
            return;

        if (enabled) ctx.PlayerMap.Enable();
        else ctx.PlayerMap.Disable();
    }

    public void StopImmediately()
    {
        output.StopImmediately(ctx);

        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
    }
}