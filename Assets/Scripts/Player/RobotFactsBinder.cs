using UnityEngine;
using UnityEngine.InputSystem;

public sealed class RobotFactsBinder
{
    public void Bind(RobotContext ctx, RobotControllerCommander commander)
    {
        BindComponents(ctx, commander);
        BindTunables(ctx, commander);
        BindInput(ctx, commander);
    }

    private void BindComponents(RobotContext ctx, RobotControllerCommander c)
    {
        ctx.Camera = c.cameraTransform;
        ctx.Animator = c.animator;

        ctx.Rb = c.GetComponent<Rigidbody>();
        ctx.Capsule = c.GetComponent<CapsuleCollider>();
        ctx.GroundCheckSphere = c.GetComponentInChildren<SphereCollider>();
        ctx.Rb.freezeRotation = true;
    }

    private void BindTunables(RobotContext ctx, RobotControllerCommander c)
    {
        ctx.MoveSpeed = c.moveSpeed;
        ctx.RunSpeed = c.moveSpeed * 1.6f;
        ctx.RotateSpeed = c.rotateSpeed;
     
        ctx.AirAccel = 12f;

        ctx.JumpHeight = 1.4f;
        ctx.CellSize = 1f;          
        ctx.JumpCellsForward = 1; 
        ctx.JumpCellsMoving = 2;  
        ctx.JumpCellsRunning = 4;

        ctx.MovingThreshold = 0.2f; // 「すでに動いてる」判定の速度( m/s )
        ctx.LockAirControl = true;  // 距離を正確にしたいなら true

        ctx.ConveyorStickTime = c.conveyorStickTime;

        ctx.GroundLayer = c.groundLayer;
        ctx.GroundCheckDistance = c.groundCheckDistance;
    }

    private void BindInput(RobotContext ctx, RobotControllerCommander c)
    {
        var asset = c.inputActions;

        ctx.PlayerMap = asset.FindActionMap("Player", true);
        ctx.MoveAction = ctx.PlayerMap.FindAction("Move", true);
        ctx.JumpAction = ctx.PlayerMap.FindAction("Jump", true);
        ctx.RunAction = ctx.PlayerMap.FindAction("Run", false);
    }
    private void ConfigureRigidbody(Rigidbody rb)
    {
        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;

        rb.interpolation = RigidbodyInterpolation.Interpolate;

        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
    }
}