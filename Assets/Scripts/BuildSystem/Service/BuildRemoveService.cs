using UnityEngine;

public sealed class BuildRemoveService
{
    private readonly BuildContext context;
    private readonly BuildState state;
    private readonly bool debugLogs;

    public BuildRemoveService(BuildContext context, BuildState state, bool debugLogs = false)
    {
        this.context = context;
        this.state = state;
        this.debugLogs = debugLogs;
    }

    // 旧API:
    // まだ app.Remove() を使っている間も壊れないように残しておく
    public bool TryRemove()
    {
        if (!TryCreateRemoveRequest(out BlockInstance target, debugLogs))
            return false;

        return TryRemoveReserved(target, debugLogs);
    }

    // 予約段階:
    // 今 raycast で見ている削除対象を確定するだけ
    public bool TryCreateRemoveRequest(out BlockInstance target, bool debugLogs = false)
    {
        target = null;

        if (!context.Raycaster.TryGetRemoveTarget(out BlockInstance hitTarget))
        {
            Log("[BuildRemoveService] TryCreateRemoveRequest: remove target not found.", debugLogs);
            return false;
        }

        if (hitTarget == null)
        {
            Log("[BuildRemoveService] TryCreateRemoveRequest: hit target is null.", debugLogs);
            return false;
        }

        target = hitTarget;
        return true;
    }

    // 確定段階:
    // 予約していた block を本当に削除する
    public bool TryRemoveReserved(BlockInstance target, bool debugLogs = false)
    {
        if (target == null)
        {
            Log("[BuildRemoveService] TryRemoveReserved: target is null.", debugLogs);
            return false;
        }

        if (target.gameObject == null)
        {
            Log("[BuildRemoveService] TryRemoveReserved: target object already destroyed.", debugLogs);
            return false;
        }

        return TryRemoveAtCell(target.OriginCell, debugLogs);
    }

    public bool TryRemoveAtCell(Vector3Int anyCell)
    {
        return TryRemoveAtCell(anyCell, debugLogs);
    }

    public bool TryRemoveAtCell(Vector3Int anyCell, bool debugLogsOverride)
    {
        RemoveCommand cmd = new RemoveCommand(context, anyCell);
        return context.History.Do(cmd, debugLogsOverride);
    }

    public bool TryRemoveBlock(BlockInstance target)
    {
        return TryRemoveReserved(target, debugLogs);
    }

    private void Log(string msg, bool enabled)
    {
        if (enabled && !string.IsNullOrEmpty(msg))
            Debug.Log(msg);
    }
}