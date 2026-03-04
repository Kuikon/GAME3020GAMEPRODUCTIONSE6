using UnityEngine;

public sealed class RobotJudgmentLogic
{
    public void Tick(RobotContext ctx)
    {
         ctx.IsGrounded = CheckGrounded(ctx);
        ComputeDesiredVelocity(ctx);
    }


    private bool CheckGrounded(RobotContext ctx)
    {
        if (ctx.GroundCheckSphere == null) return false;

        Transform t = ctx.GroundCheckSphere.transform;
        Vector3 worldCenter = t.TransformPoint(ctx.GroundCheckSphere.center);

        float scale = Mathf.Max(t.lossyScale.x, t.lossyScale.z);
        float worldRadius = ctx.GroundCheckSphere.radius * scale;

        bool hitSomething = Physics.SphereCast(
            worldCenter,
            worldRadius,
            Vector3.down,
            out RaycastHit hit,
            ctx.GroundCheckDistance,
            ctx.GroundLayer,
            QueryTriggerInteraction.Ignore
        );

        // 中心から下へ線（当たったら hit.point まで）
        Vector3 end = hitSomething ? hit.point : (worldCenter + Vector3.down * ctx.GroundCheckDistance);
        Debug.DrawLine(worldCenter, end, hitSomething ? Color.green : Color.red);

        const float minGroundNormalY = 0.7f;
        return hitSomething && hit.normal.y >= minGroundNormalY;
    }

    private void ComputeDesiredVelocity(RobotContext ctx)
    {
        Vector3 rbV = ctx.Rb.linearVelocity;
        float speed = ctx.RunHeld ? ctx.RunSpeed : ctx.MoveSpeed;
        Vector3 horizontal = ctx.MoveDir * speed;

        // 入力なし & 地面 → コンベアのみ
        if (ctx.IsGrounded && ctx.MoveInput.sqrMagnitude < 0.01f)
        {
            ctx.DesiredVelocity = new Vector3(ctx.ConveyorVelocity.x, rbV.y, ctx.ConveyorVelocity.z);
            return;
        }

        Vector3 desired = new Vector3(horizontal.x, rbV.y, horizontal.z);
        desired += ctx.ConveyorVelocity;

        ctx.DesiredVelocity = desired;
    }
}