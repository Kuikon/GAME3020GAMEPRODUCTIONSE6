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
    public bool CanDoubleJump;

    public float ConveyorStickTime;

    public LayerMask GroundLayer;
    public float GroundCheckDistance;
    public float CoyoteTime;

    public float WallCheckDistance;

    // ========= STATE =========
    public Vector2 MoveInput;
    public bool JumpPressed;

    public bool IsGrounded;
    public bool UsedDoubleJump;

    public bool IsWallContact;
    public Vector3 WallNormal;

    public Vector3 ConveyorVelocity;
    public float ConveyorTimer;

    public float UngroundTimer;

    // ========= TRANSFORM cache =========
    public Vector3 MoveDir;         
    public Vector3 DesiredVelocity; 
    public Quaternion DesiredRotation;
    public bool HasDesiredRotation;

    // ========= FRAME =========
    public float Dt; // FixedUpdate‚Ìdt
}