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

        ctx.Rb.freezeRotation = true;
    }

    private void BindTunables(RobotContext ctx, RobotControllerCommander c)
    {
        ctx.MoveSpeed = c.moveSpeed;
        ctx.RotateSpeed = c.rotateSpeed;

        ctx.JumpForce = c.jumpForce;

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
    }
}