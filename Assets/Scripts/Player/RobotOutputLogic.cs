using UnityEngine;

public sealed class RobotOutputLogic
{
    public void Tick(RobotContext ctx)
    {
        ApplyHorizontalMovement(ctx);
        ApplyRotation(ctx);
        ApplyJump(ctx);
        ApplyBetterGravity(ctx);
        UpdateAnimator(ctx);
    }

    private void ApplyHorizontalMovement(RobotContext ctx)
    {
        Vector3 current = ctx.Rb.linearVelocity;
        Vector3 currentXZ = new Vector3(current.x, 0f, current.z);

        Vector3 targetXZ = new Vector3(ctx.DesiredVelocity.x, 0f, ctx.DesiredVelocity.z);
        targetXZ += new Vector3(ctx.ConveyorVelocity.x, 0f, ctx.ConveyorVelocity.z);

        float accel = ctx.IsGrounded ? ctx.GroundAccel : ctx.AirAccel;
        Vector3 nextXZ = Vector3.MoveTowards(currentXZ, targetXZ, accel * ctx.Dt);

        ctx.Rb.linearVelocity = new Vector3(nextXZ.x, current.y, nextXZ.z);
    }

    private void ApplyRotation(RobotContext ctx)
    {
        if (!ctx.HasDesiredRotation) return;

        Quaternion next = Quaternion.Slerp(
            ctx.Rb.rotation,
            ctx.DesiredRotation,
            ctx.RotateSpeed * ctx.Dt
        );

        ctx.Rb.MoveRotation(next);
    }

    private void ApplyJump(RobotContext ctx)
    {
        if (ctx.JumpRequestedThisFrame)
        {
            Vector3 v = ctx.Rb.linearVelocity;
            v.y = ctx.JumpForce;
            ctx.Rb.linearVelocity = v;
            ctx.IsGrounded = false;
            if (ctx.Animator)
                ctx.Animator.SetTrigger("Jump");
        }

        if (ctx.JumpCutRequestedThisFrame)
        {
            Vector3 v = ctx.Rb.linearVelocity;
            if (v.y > 0f)
            {
                v.y *= ctx.JumpCutMultiplier;
                ctx.Rb.linearVelocity = v;
            }
        }
    }

    private void ApplyBetterGravity(RobotContext ctx)
    {
        Vector3 v = ctx.Rb.linearVelocity;

        if (v.y < 0f)
        {
            v += Physics.gravity * (ctx.FallGravityMultiplier - 1f) * ctx.Dt;
        }
        else if (v.y > 0f && !ctx.JumpHeld)
        {
            v += Physics.gravity * (ctx.LowJumpGravityMultiplier - 1f) * ctx.Dt;
        }

        ctx.Rb.linearVelocity = v;
    }

    private void UpdateAnimator(RobotContext ctx)
    {
        if (!ctx.Animator) return;

        Vector3 v = ctx.Rb.linearVelocity;
        float speed = new Vector3(v.x, 0f, v.z).magnitude;

        ctx.Animator.SetFloat("Speed", speed);
        ctx.Animator.SetBool("IsGrounded", ctx.IsGrounded);
        ctx.Animator.SetFloat("VerticalSpeed", v.y);
    }

    public void StopImmediately(RobotContext ctx)
    {
        ctx.Rb.linearVelocity = Vector3.zero;
    }
}