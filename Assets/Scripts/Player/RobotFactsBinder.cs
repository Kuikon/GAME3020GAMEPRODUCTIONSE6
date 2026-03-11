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
        ctx.RunSpeed = c.runSpeed;
        ctx.RotateSpeed = c.rotateSpeed;
        ctx.GroundAccel = c.groundAccel;
        ctx.AirAccel = c.airAccel;

        ctx.JumpHeight = c.jumpHeight;
        ctx.JumpBufferTime = c.jumpBufferTime;
        ctx.CoyoteTime = c.coyoteTime;
        ctx.JumpCutMultiplier = c.jumpCutMultiplier;
        ctx.FallGravityMultiplier = c.fallGravityMultiplier;
        ctx.LowJumpGravityMultiplier = c.lowJumpGravityMultiplier;

        ctx.ConveyorStickTime = c.conveyorStickTime;

        ctx.GroundLayer = c.groundLayer;
        ctx.GroundCheckDistance = c.groundCheckDistance;
        ctx.GroundCheckRadius = c.groundCheckRadius;
    }

    private void BindInput(RobotContext ctx, RobotControllerCommander c)
    {
        var map = c.inputActions.FindActionMap("Player", true);

        ctx.PlayerMap = map;
        ctx.MoveAction = map.FindAction("Move", true);
        ctx.RunAction = map.FindAction("Run", false);
        ctx.JumpAction = map.FindAction("Jump", true);
    }
}