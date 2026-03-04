using UnityEngine;

public sealed class RobotStateLogic
{
    public void Tick(RobotContext ctx)
    {
        TickConveyor(ctx);
        TickJumpRequest(ctx);
        TickFixedJumpState(ctx);
    }

    private void TickConveyor(RobotContext ctx)
    {
        if (ctx.ConveyorTimer <= 0f) return;

        ctx.ConveyorTimer -= ctx.Dt;
        if (ctx.ConveyorTimer <= 0f)
            ctx.ConveyorVelocity = Vector3.zero;
    }
    private void TickJumpRequest(RobotContext ctx)
    {
        if (!ctx.JumpPressed) return;
        ctx.JumpPressed = false;

        if (!CanJump(ctx)) return;

        int cells = DecideJumpCells(ctx);
        StartFixedJump(ctx, cells);
        Debug.Log($"Jump cells={cells} run={ctx.RunHeld} moveDirMag={ctx.MoveDir.magnitude} planarSpeed={GetPlanarSpeed(ctx)} grounded={ctx.IsGrounded}");
        // OutputでY初速を入れるための要求
        ctx._jumpToExecute = true;
    }
    private bool CanJump(RobotContext ctx)
    {
        return ctx.IsGrounded;
    }
    private int DecideJumpCells(RobotContext ctx)
    {
        // 入力なし → その場ジャンプ(0マス)
        if (ctx.MoveDir.sqrMagnitude < 0.0001f)
            return 0;

        // 走り → 4マス
        if (ctx.RunHeld)
            return ctx.JumpCellsRunning;

        // すでに歩いてる判定
        bool alreadyMoving = GetPlanarSpeed(ctx) > ctx.MovingThreshold;

        // 動いてる → 2マス、止まってる → 1マス
        return alreadyMoving ? ctx.JumpCellsMoving : ctx.JumpCellsForward;
    }
    private float GetPlanarSpeed(RobotContext ctx)
    {
        Vector3 v = ctx.Rb.linearVelocity;
        v.y = 0f;
        return v.magnitude;
    }
    private void StartFixedJump(RobotContext ctx, int cells)
    {
        float g = Mathf.Abs(Physics.gravity.y);

        // JumpForce = 上向き初速（m/s）
        ctx.JumpForce = Mathf.Sqrt(2f * g * ctx.JumpHeight);

        // 空中時間（上り+下り）見積もり
        ctx.FixedJumpDuration = Mathf.Max(0.05f, (2f * ctx.JumpForce) / g);
        ctx.FixedJumpTime = 0f;

        // Start XZ
        Vector3 pos = ctx.Rb.position;
        ctx.FixedJumpStartXZ = new Vector3(pos.x, 0f, pos.z);

        // Target XZ（cells * cellSize）
        float dist = cells * ctx.CellSize;

        Vector3 dir = ctx.MoveDir;
        dir.y = 0f;
        if (dir.sqrMagnitude > 1f) dir.Normalize();

        ctx.FixedJumpTargetXZ = ctx.FixedJumpStartXZ + dir * dist;

        ctx.FixedJumpActive = true;
    }
    private void TickFixedJumpState(RobotContext ctx)
    {
        if (!ctx.FixedJumpActive) return;

        ctx.FixedJumpTime += ctx.Dt;

        // ジャンプ直後の接地ブレ対策
        if (ctx.FixedJumpTime < 0.05f) return;
        if (ctx.Rb.linearVelocity.y > 0.01f) return;
        if (!ctx.IsGrounded) return;

        ctx.FixedJumpActive = false;
    }
}