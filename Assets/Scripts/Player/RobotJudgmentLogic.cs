using UnityEngine;

public sealed class RobotJudgmentLogic
{
    public void Tick(RobotContext ctx)
    {
         ctx.IsGrounded = CheckGrounded(ctx);
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

        // ’†S‚©‚ç‰º‚Öüi“–‚½‚Á‚½‚ç hit.point ‚Ü‚Åj
        Vector3 end = hitSomething ? hit.point : (worldCenter + Vector3.down * ctx.GroundCheckDistance);
        Debug.DrawLine(worldCenter, end, hitSomething ? Color.green : Color.red);

        const float minGroundNormalY = 0.7f;
        return hitSomething && hit.normal.y >= minGroundNormalY;
    }

  
}