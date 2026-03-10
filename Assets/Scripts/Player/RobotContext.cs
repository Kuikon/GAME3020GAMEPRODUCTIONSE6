using UnityEngine;
using UnityEngine.InputSystem;

public sealed class RobotContext
{
    // =========================================================
    // FACT (Refs)
    // =========================================================
    public Transform Camera;
    public Rigidbody Rb;
    public CapsuleCollider Capsule;
    public SphereCollider GroundCheckSphere;
    public Animator Animator;

    // Input System
    public InputActionMap PlayerMap;
    public InputAction MoveAction;
    public InputAction JumpAction;
    public InputAction RunAction;

    // =========================================================
    // FACT (Tunables)
    // =========================================================
    public float MoveSpeed;
    public float RunSpeed;
    public float RotateSpeed;
    public float GroundAccel;
    public float AirAccel;

    // Jump (Height-based recommended)
    public float JumpHeight;      // desired jump height (meters)
    public float JumpForce;       // computed initial Y velocity from JumpHeight

    // Conveyor
    public float ConveyorStickTime;

    // Ground check
    public LayerMask GroundLayer;
    public float GroundCheckDistance;

    // =========================================================
    // FACT (Fixed Distance Jump Tunables)
    // =========================================================
    public float CellSize;            // 1 cell = how many meters
    public int JumpCellsForward;      // jump while pressing forward (standing still) -> 1
    public int JumpCellsMoving;       // jump while already moving -> 2
    public int JumpCellsRunning;      // jump while running -> 4
    public float MovingThreshold;     // planar speed threshold to detect "already moving"
    public bool LockAirControl;       // lock air control to keep distance exact

    // =========================================================
    // STATE (Inputs / Flags)
    // =========================================================
    public Vector2 MoveInput;
    public bool JumpPressed;          // set by input callback, consumed by StateLogic
    public bool RunHeld;              // set each frame from RunAction or key

    // Output request flags
    public bool _jumpToExecute;       // consumed by OutputLogic (apply Y jump)

    // =========================================================
    // STATE (World / Movement)
    // =========================================================
    public bool IsGrounded;
    public Vector3 GroundNormal;

    public Vector3 ConveyorVelocity;
    public float ConveyorTimer;

    // =========================================================
    // TRANSFORM / CACHE
    // =========================================================
    public Vector3 MoveDir;            // camera-relative world XZ direction (normalized)
    public Quaternion DesiredRotation;
    public bool HasDesiredRotation;

    public Vector3 DesiredVelocity;   // full desired velocity (x,z driven by systems; y typically preserved)

    // =========================================================
    // STATE (Fixed Distance Jump Runtime)
    // =========================================================
    public bool FixedJumpActive;
    public float FixedJumpTime;
    public float FixedJumpDuration;
    public Vector3 FixedJumpStartXZ;   // y=0 stored
    public Vector3 FixedJumpTargetXZ;  // y=0 stored

    public Vector3 FixedJumpDirectionXZ;
    // =========================================================
    // FRAME
    // =========================================================
    public float Dt; // FixedUpdate delta time
}