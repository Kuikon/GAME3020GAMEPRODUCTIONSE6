using UnityEngine;

public sealed class RobotOutputLogic
{
    public void Tick(RobotContext ctx)
    {
        ApplyHorizontalMovement(ctx);
        ApplyRotation(ctx);
        ApplyJump(ctx);
        ApplyBetterGravity(ctx);

        PlayLandingSound(ctx);
        //PlayFootstepSound(ctx);
        PlayDashSound(ctx);
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
            if (SoundManager.Instance != null)
                SoundManager.Instance.PlaySE(SESoundData.SE.Jump);
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
    private void PlayLandingSound(RobotContext ctx)
    {
        if (!ctx.WasGrounded && ctx.IsGrounded)
        {
            if (SoundManager.Instance != null)
                SoundManager.Instance.PlaySE(SESoundData.SE.Land);

            // prevent instant footstep right after landing
            ctx.FootstepTimer = ctx.FootstepAfterLandDelay;
        }
    }
    private void PlayFootstepSound(RobotContext ctx)
    {
        if (ctx.IsDashing)
            return;

        Vector3 v = ctx.Rb.linearVelocity;
        float horizontalSpeed = new Vector3(v.x, 0f, v.z).magnitude;

        bool isMovingOnGround = ctx.IsGrounded && horizontalSpeed > 0.2f;

        if (!isMovingOnGround)
        {
            // do not make it 0, or it plays instantly next time
            ctx.FootstepTimer = ctx.FootstepStartDelay;
            return;
        }

        ctx.FootstepTimer -= ctx.Dt;

        if (ctx.FootstepTimer > 0f)
            return;

        SoundManager.Instance?.PlaySE(SESoundData.SE.Footstep);

        float maxSpeed = Mathf.Max(ctx.MoveSpeed, ctx.RunSpeed);
        float speed01 = Mathf.InverseLerp(0f, maxSpeed, horizontalSpeed);

        float interval = Mathf.Lerp(
            ctx.MaxFootstepInterval,
            ctx.MinFootstepInterval,
            speed01
        );

        // extra safety clamp
        ctx.FootstepTimer = Mathf.Clamp(
            interval,
            ctx.MinFootstepInterval,
            ctx.MaxFootstepInterval
        );
    }
    private void PlayDashSound(RobotContext ctx)
    {
        if (ctx.DashStartedThisFrame)
        {
            SoundManager.Instance?.PlaySE(SESoundData.SE.Dash);
        }
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