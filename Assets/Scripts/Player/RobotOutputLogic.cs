using UnityEngine;

public sealed class RobotOutputLogic
{
    public void Tick(RobotContext ctx)
    {
        ApplyMovement(ctx);
        ApplyRotation(ctx);
        UpdateAnimator(ctx);
        ApplyJumpIfRequested(ctx);
    }

    private void ApplyMovement(RobotContext ctx)
    {
        ctx.Rb.linearVelocity = ctx.DesiredVelocity;
    }

    private void ApplyRotation(RobotContext ctx)
    {
        if (!ctx.HasDesiredRotation) return;

        Quaternion next = Quaternion.Slerp(ctx.Rb.rotation, ctx.DesiredRotation, ctx.RotateSpeed * ctx.Dt);
        ctx.Rb.MoveRotation(next);
    }

    private void ApplyJumpIfRequested(RobotContext ctx)
    {
        if (!ctx._jumpToExecute) return;
        ctx._jumpToExecute = false;

        Vector3 v = ctx.Rb.linearVelocity;
        v.y = ctx.JumpForce;
        ctx.Rb.linearVelocity = v;

        if (ctx.Animator)
            ctx.Animator.SetTrigger("Jump");
    }

    private void UpdateAnimator(RobotContext ctx)
    {
        if (!ctx.Animator) return;

        Vector3 v = ctx.Rb.linearVelocity;
        float speed = new Vector3(v.x, 0f, v.z).magnitude;

        ctx.Animator.SetFloat("Speed", speed);
        ctx.Animator.SetBool("IsGrounded", ctx.IsGrounded);
    }

    public void StopImmediately(RobotContext ctx)
    {
        ctx.ConveyorVelocity = Vector3.zero;
        ctx.ConveyorTimer = 0f;

        ctx.Rb.linearVelocity = Vector3.zero;
        ctx.Rb.angularVelocity = Vector3.zero;
    }
}