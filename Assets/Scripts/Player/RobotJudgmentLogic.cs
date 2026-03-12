using UnityEngine;

public sealed class RobotJudgmentLogic
{
    public void Tick(RobotContext ctx)
    {
        ctx.WasGrounded = ctx.IsGrounded;
        ctx.IsGrounded = CheckGrounded(ctx);
        ComputeDesiredVelocity(ctx);
    }

    private bool CheckGrounded(RobotContext ctx)
    {
        if (ctx.GroundCheckSphere == null) return false;
         
        Transform t = ctx.GroundCheckSphere.transform;

        // SphereCollider の中心をワールド座標に変換
        Vector3 worldCenter = t.TransformPoint(ctx.GroundCheckSphere.center);

        // scale を考慮した半径
        float scale = Mathf.Max(t.lossyScale.x, t.lossyScale.z);
        float worldRadius = ctx.GroundCheckSphere.radius * scale;

        bool hit = Physics.CheckSphere(
            worldCenter,
            worldRadius,
            ctx.GroundLayer,
            QueryTriggerInteraction.Ignore
        );
        if (ctx.Rb.linearVelocity.y > 0.05f)
            return false;
        Debug.DrawLine(
            worldCenter,
            worldCenter + Vector3.down * 0.2f,
            hit ? Color.green : Color.red
        );

        return hit;
    }
    private void ComputeDesiredVelocity(RobotContext ctx)
    {
        float targetSpeed = ctx.RunHeld ? ctx.RunSpeed : ctx.MoveSpeed;
        Vector3 move = ctx.MoveDir * targetSpeed;

        ctx.DesiredVelocity = new Vector3(move.x, ctx.Rb.linearVelocity.y, move.z);
    }
}