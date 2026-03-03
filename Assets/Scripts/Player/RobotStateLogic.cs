using UnityEngine;

public sealed class RobotStateLogic
{
    public void Tick(RobotContext ctx)
    {
        TickConveyor(ctx);
    }

    private void TickConveyor(RobotContext ctx)
    {
        if (ctx.ConveyorTimer <= 0f) return;

        ctx.ConveyorTimer -= ctx.Dt;
        if (ctx.ConveyorTimer <= 0f)
            ctx.ConveyorVelocity = Vector3.zero;
    }
}