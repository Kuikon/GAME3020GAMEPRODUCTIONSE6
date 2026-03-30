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

    // —\–ñ’iŠK:
    // ¡ raycast ‚ÅŒ©‚Ä‚¢‚éíœ‘ÎÛ‚ğŠm’è‚·‚é‚¾‚¯
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

    // Šm’è’iŠK:
    // —\–ñ‚µ‚Ä‚¢‚½ block ‚ğ–{“–‚Éíœ‚·‚é
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

    public bool TryRemoveAtCell(Vector3Int anyCell, bool debugLogsOverride)
    {
        RemoveCommand cmd = new RemoveCommand(context, anyCell);
        return context.History.Do(cmd, debugLogsOverride, playEffects: true);
    }

    private void Log(string msg, bool enabled)
    {
        if (enabled && !string.IsNullOrEmpty(msg))
            Debug.Log(msg);
    }
}