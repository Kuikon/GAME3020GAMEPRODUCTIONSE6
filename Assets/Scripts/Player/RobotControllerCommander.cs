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

    [Header("Movement")]
    public float moveSpeed = 5f;
    public float rotateSpeed = 12f;


    [Header("Animation")]
    public Animator animator;

    [Header("Conveyor")]
    public float conveyorStickTime = 0.1f;

    [Header("Ground")]
    public LayerMask groundLayer;
    public float groundCheckDistance = 0.08f;

    private RobotContext ctx;

    private readonly RobotFactsBinder facts = new RobotFactsBinder();
    private readonly RobotStateLogic state = new RobotStateLogic();
    private readonly RobotTransformLogic xform = new RobotTransformLogic();
    private readonly RobotJudgmentLogic judge = new RobotJudgmentLogic();
    private readonly RobotOutputLogic output = new RobotOutputLogic();

    private System.Action<InputAction.CallbackContext> jumpHandler;

    private void Awake()
    {
        ctx = new RobotContext();
        facts.Bind(ctx, this);

        jumpHandler = OnJumpPerformed;
    }

    private void OnEnable()
    {
        ctx.PlayerMap.Enable();
        ctx.JumpAction.performed += jumpHandler;
    }

    private void OnDisable()
    {
        ctx.JumpAction.performed -= jumpHandler;
        ctx.PlayerMap.Disable();
    }

    private void Update()
    {
        // FACT: 入力（Updateで読む）
        ctx.MoveInput = ctx.MoveAction.ReadValue<Vector2>();
        if (ctx.RunAction != null)
            ctx.RunHeld = ctx.RunAction.ReadValue<float>() > 0.5f;
        else
            ctx.RunHeld = false;
    }

    private void FixedUpdate()
    {
        ctx.Dt = Time.fixedDeltaTime;

        // 役割の順番だけ決めて呼ぶ
        judge.Tick(ctx);
        state.Tick(ctx);
        xform.Tick(ctx);
   
        output.Tick(ctx);
    }

    private void OnJumpPerformed(InputAction.CallbackContext _)
    {
        // Update/FixedどっちでもOKだが、物理はFixedで処理するのでフラグだけ立てる
        ctx.JumpPressed = true;
    }

    // ===== Public API =====
    public void SetConveyorVelocity(Vector3 velocity)
    {
        ctx.ConveyorVelocity = velocity;
        ctx.ConveyorTimer = ctx.ConveyorStickTime;
    }

    public void SetInputEnabled(bool enabled)
    {
        if (enabled) ctx.PlayerMap.Enable();
        else ctx.PlayerMap.Disable();
    }

    public void StopImmediately()
    {
        output.StopImmediately(ctx);
    }
}