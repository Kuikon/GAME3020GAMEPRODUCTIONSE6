using UnityEngine;
using UnityEngine.InputSystem;

public sealed class RobotContext
{
    // ========= FACT (refs) =========
    public Transform Camera;
    public Rigidbody Rb;
    public CapsuleCollider Capsule;
    public Animator Animator;

    public InputActionMap PlayerMap;
    public InputAction MoveAction;
    public InputAction JumpAction;

    // ========= FACT (tunables) =========
    public float MoveSpeed;
    public float RotateSpeed;

    public float JumpForce;

    public float ConveyorStickTime;

    public LayerMask GroundLayer;
    public float GroundCheckDistance;

    // ========= STATE =========
    public Vector2 MoveInput;
    public bool JumpPressed;
    public bool _jumpToExecute;
    public bool IsGrounded;
    public Vector3 WallNormal;

    public Vector3 ConveyorVelocity;
    public float ConveyorTimer;

    // ========= TRANSFORM cache =========
    public Vector3 MoveDir;         
    public Vector3 DesiredVelocity; 
    public Quaternion DesiredRotation;
    public bool HasDesiredRotation;

    // ========= FRAME =========
    public float Dt; // FixedUpdate‚Ìdt
}