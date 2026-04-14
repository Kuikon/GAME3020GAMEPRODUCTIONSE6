using UnityEngine;
using UnityEngine.InputSystem;

public class RobotContext
{
    // Components
    public Transform Camera;
    public Animator Animator;
    public Rigidbody Rb;
    public CapsuleCollider Capsule;
    public SphereCollider GroundCheckSphere;

    // Input
    public InputActionMap PlayerMap;
    public InputAction MoveAction;
    public InputAction RunAction;
    public InputAction JumpAction;

    public Vector2 MoveInput;
    public bool RunHeld;
    public bool JumpPressed;
    public bool JumpHeld;
    public bool JumpReleased;

    // Time
    public float Dt;

    // Movement tunables
    public float MoveSpeed;
    public float RunSpeed;
    public float RotateSpeed;
    public float GroundAccel;
    public float AirAccel;

    // Jump tunables
    public float JumpHeight;
    public float JumpForce;
    public float JumpBufferTime;
    public float JumpBufferTimer;
    public float CoyoteTime;
    public float CoyoteTimer;
    public float JumpCutMultiplier;
    public float FallGravityMultiplier;
    public float LowJumpGravityMultiplier;

    // Ground
    public LayerMask GroundLayer;
    public float GroundCheckDistance;
    public bool IsGrounded;
    public bool WasGrounded;

    // Conveyor
    public Vector3 ConveyorVelocity;
    public float ConveyorTimer;
    public float ConveyorStickTime;

    // Movement state
    public Vector3 MoveDir;
    public Vector3 DesiredVelocity;
    public Quaternion DesiredRotation;
    public bool HasDesiredRotation;

    // Jump state
    public bool JumpRequestedThisFrame;
    public bool JumpCutRequestedThisFrame;
    public Transform GroundPoint;
    public float GroundCheckRadius;

    // Sound
    public float FootstepTimer;
    public float WalkFootstepInterval = 0.4f;
    public float RunFootstepInterval = 0.28f;
    public float MinFootstepInterval = 0.22f;
    public float MaxFootstepInterval = 0.5f;
    public bool DashStartedThisFrame;
    public bool IsDashing;
    public float FootstepStartDelay = 0.08f;
    public float FootstepAfterLandDelay = 0.12f;
}