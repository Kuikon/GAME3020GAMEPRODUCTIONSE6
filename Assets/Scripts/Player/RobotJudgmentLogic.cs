using UnityEngine;

public sealed class RobotJudgmentLogic
{
    public void Tick(RobotContext ctx)
    {
        CheckGrounded(ctx);
        ConsumeJumpIfAny(ctx);
        ComputeDesiredVelocity(ctx);
    }


    private bool CheckGrounded(RobotContext ctx)
    {
        GetCapsuleWorld(ctx, out var p1, out var p2, out float radius);

        return Physics.CapsuleCast(
            p1, p2, radius * 0.98f,
            Vector3.down,
            out _,
            ctx.GroundCheckDistance,
            ctx.GroundLayer,
            QueryTriggerInteraction.Ignore
        );
    }

    private void ConsumeJumpIfAny(RobotContext ctx)
    {
        if (!ctx.JumpPressed) return;

        ctx.JumpPressed = false;

        if (!CanJump(ctx)) return;

        ctx._jumpToExecute = true;
    }

    private bool CanJump(RobotContext ctx)
    {
        if (ctx.IsGrounded) return true;
        return true;
    }

    private void ComputeDesiredVelocity(RobotContext ctx)
    {
        Vector3 v = ctx.Rb.linearVelocity;

        if (ctx.IsGrounded)
            v.y = Mathf.Min(v.y, 0f);

        Vector3 horizontal = ctx.MoveDir * ctx.MoveSpeed;

        // 入力なし & 地面 → コンベアのみ
        if (ctx.IsGrounded && ctx.MoveInput.sqrMagnitude < 0.01f)
        {
            ctx.DesiredVelocity = new Vector3(ctx.ConveyorVelocity.x, v.y, ctx.ConveyorVelocity.z);
            return;
        }

        Vector3 desired = new Vector3(horizontal.x, v.y, horizontal.z);
        desired += ctx.ConveyorVelocity;

        ctx.DesiredVelocity = desired;
    }

    private void GetCapsuleWorld(RobotContext ctx, out Vector3 p1, out Vector3 p2, out float radius)
    {
        var capsule = ctx.Capsule;
        Transform t = capsule.transform;

        Vector3 center = t.TransformPoint(capsule.center);

        float r = capsule.radius * Mathf.Max(t.lossyScale.x, t.lossyScale.z);
        float height = Mathf.Max(capsule.height * t.lossyScale.y, r * 2f);
        float half = (height * 0.5f) - r;

        p1 = center + Vector3.up * half;
        p2 = center - Vector3.up * half;
        radius = r;
    }

    // ===== “ジャンプ実行要求”を ctx に持たせるための小技（引数増やさない） =====
    // RobotContext に private を置けないので partial でも良いが、最小でいくため
    // extensionっぽく公開フィールドにする
}