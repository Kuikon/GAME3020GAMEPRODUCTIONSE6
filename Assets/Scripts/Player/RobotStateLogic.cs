using UnityEngine;

public sealed class RobotStateLogic
{
    public void Tick(RobotContext ctx)
    {
        TickConveyor(ctx);
        TickJumpBuffer(ctx);
        TickCoyoteTime(ctx);
        TickJumpRequest(ctx);
        TickJumpRelease(ctx);
    }

    private void TickConveyor(RobotContext ctx)
    {
        if (ctx.ConveyorTimer <= 0f) return;

        ctx.ConveyorTimer -= ctx.Dt;
        if (ctx.ConveyorTimer <= 0f)
            ctx.ConveyorVelocity = Vector3.zero;
    }

    private void TickJumpBuffer(RobotContext ctx)
    {
        if (ctx.JumpPressed)
        {
            ctx.JumpBufferTimer = ctx.JumpBufferTime;
            ctx.JumpPressed = false;
        }
        else if (ctx.JumpBufferTimer > 0f)
        {
            ctx.JumpBufferTimer -= ctx.Dt;
        }
    }

    private void TickCoyoteTime(RobotContext ctx)
    {
        if (ctx.IsGrounded)
            ctx.CoyoteTimer = ctx.CoyoteTime;
        else if (ctx.CoyoteTimer > 0f)
            ctx.CoyoteTimer -= ctx.Dt;
    }

    private void TickJumpRequest(RobotContext ctx)
    {
        ctx.JumpRequestedThisFrame = false;

        if (!CanJump(ctx)) return;

        ctx.JumpBufferTimer = 0f;
        ctx.CoyoteTimer = 0f;
        ctx.JumpRequestedThisFrame = true;

        float g = Mathf.Abs(Physics.gravity.y);
        ctx.JumpForce = Mathf.Sqrt(2f * g * ctx.JumpHeight);
    }

    private bool CanJump(RobotContext ctx)
    {
        return ctx.JumpBufferTimer > 0f && ctx.CoyoteTimer > 0f;
    }

    private void TickJumpRelease(RobotContext ctx)
    {
        ctx.JumpCutRequestedThisFrame = false;

        if (!ctx.JumpReleased) return;
        ctx.JumpReleased = false;

        if (ctx.Rb.linearVelocity.y > 0f)
            ctx.JumpCutRequestedThisFrame = true;
    }
}