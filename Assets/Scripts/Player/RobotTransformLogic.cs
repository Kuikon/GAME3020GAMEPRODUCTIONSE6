using UnityEngine;

public sealed class RobotTransformLogic
{
    public void Tick(RobotContext ctx)
    {
        ComputeMoveDir(ctx);
        ComputeRotationTarget(ctx);
        ComputeDesiredVelocity(ctx);
        ApplyFixedJumpXZToDesiredVelocity(ctx);
    }

    private void ComputeMoveDir(RobotContext ctx)
    {
        ctx.MoveDir = CameraRelativeDirection(ctx);
    }

    private Vector3 CameraRelativeDirection(RobotContext ctx)
    {
        Vector2 input = ctx.MoveInput;

        if (!ctx.Camera)
            return new Vector3(input.x, 0f, input.y).normalized;

        Vector3 forward = ctx.Camera.forward;
        Vector3 right = ctx.Camera.right;

        forward.y = 0f;
        right.y = 0f;

        Vector3 dir = forward.normalized * input.y + right.normalized * input.x;
        return dir.sqrMagnitude > 0.0001f ? dir.normalized : Vector3.zero;
    }

    private void ComputeRotationTarget(RobotContext ctx)
    {
        ctx.HasDesiredRotation = false;

        if (ctx.MoveInput.sqrMagnitude < 0.01f) return;
        if (ctx.MoveDir.sqrMagnitude < 0.0001f) return;

        ctx.DesiredRotation = Quaternion.LookRotation(ctx.MoveDir, Vector3.up);
        ctx.HasDesiredRotation = true;
    }
    private void ComputeDesiredVelocity(RobotContext ctx)
    {
        float speed = ctx.RunHeld ? ctx.RunSpeed : ctx.MoveSpeed;

        // 地上
        if (!ctx.FixedJumpActive)
        {
            Vector3 move = ctx.MoveDir * speed;

            ctx.DesiredVelocity = new Vector3(
                move.x + ctx.ConveyorVelocity.x,
                ctx.Rb.linearVelocity.y,
                move.z + ctx.ConveyorVelocity.z
            );
            return;
        }

        // 空中（固定ジャンプ + 少しだけ補正）
        Vector3 fixedMove = ctx.FixedJumpDirectionXZ * speed;
        Vector3 steerMove = ctx.MoveDir * (speed * 0.3f);

        Vector3 targetPlanar = fixedMove + steerMove;
        Vector3 current = ctx.Rb.linearVelocity;
        Vector3 currentPlanar = new Vector3(current.x, 0f, current.z);

        // いきなり切り替えず、少しずつ寄せる
        Vector3 nextPlanar = Vector3.Lerp(currentPlanar, targetPlanar, 8f * ctx.Dt);

        ctx.DesiredVelocity = new Vector3(
            nextPlanar.x + ctx.ConveyorVelocity.x,
            ctx.Rb.linearVelocity.y,
            nextPlanar.z + ctx.ConveyorVelocity.z
        );
    }
    private void ApplyFixedJumpXZToDesiredVelocity(RobotContext ctx)
    {
        if (!ctx.FixedJumpActive) return;

        float t = ctx.FixedJumpDuration <= 0f ? 1f : Mathf.Clamp01(ctx.FixedJumpTime / ctx.FixedJumpDuration);

        Vector3 desiredXZ = Vector3.Lerp(ctx.FixedJumpStartXZ, ctx.FixedJumpTargetXZ, t);

        Vector3 p = ctx.Rb.position;
        Vector3 currentXZ = new Vector3(p.x, 0f, p.z);

        Vector3 delta = desiredXZ - currentXZ;
        Vector3 neededVelXZ = delta / Mathf.Max(0.0001f, ctx.Dt);

        if (ctx.LockAirControl)
        {
            ctx.DesiredVelocity = new Vector3(neededVelXZ.x, ctx.DesiredVelocity.y, neededVelXZ.z);
            return;
        }
    }
}