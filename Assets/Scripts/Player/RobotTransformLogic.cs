using UnityEngine;

public sealed class RobotTransformLogic
{
    public void Tick(RobotContext ctx)
    {
        ComputeMoveDir(ctx);
        ComputeRotationTarget(ctx);
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

        forward.Normalize();
        right.Normalize();

        Vector3 dir = forward * input.y + right * input.x;
        return dir.sqrMagnitude > 0.0001f ? dir.normalized : Vector3.zero;
    }

    private void ComputeRotationTarget(RobotContext ctx)
    {
        ctx.HasDesiredRotation = false;

        if (ctx.MoveDir.sqrMagnitude < 0.0001f) return;

        ctx.DesiredRotation = Quaternion.LookRotation(ctx.MoveDir, Vector3.up);
        ctx.HasDesiredRotation = true;
    }
}