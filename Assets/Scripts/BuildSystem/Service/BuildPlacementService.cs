using System.Collections.Generic;
using UnityEngine;

public sealed class BuildPlacementService
{
    private readonly BuildContext context;
    private readonly BuildState state;
    private readonly bool debugLogs;

    public BuildPlacementService(BuildContext context, BuildState state, bool debugLogs = false)
    {
        this.context = context;
        this.state = state;
        this.debugLogs = debugLogs;
    }

    public void UpdatePreview()
    {
        if (state.PlaceTool == BuildTool.Move)
        {
            UpdateMovePreview();
            return;
        }

        if (!TryGetSelectedData(out ObjectData data))
        {
            HidePreviewAndIdleDrone();
            return;
        }

        context.Preview.SetSelected(data);

        Quaternion rotation = state.CurrentRotation;
        Vector3Int rotatedSize = context.Solver.GetRotatedSize(data.SizeXYZ, rotation);

        if (!context.Solver.TryGetHoverCell(
                context.Raycaster,
                rotatedSize,
                out Vector3Int hoverCell,
                out _))
        {
            HidePreviewAndIdleDrone();
            return;
        }

        if (state.PlaceTool == BuildTool.Line && state.HasLineStart)
        {
            UpdateLinePreview(data, rotation, rotatedSize, hoverCell);
            return;
        }

        bool canPlace = context.Rules.CanPlaceObject(
            data,
            context.Database,
            hoverCell,
            rotatedSize,
            out _);

        context.Preview.ShowSingle(hoverCell, rotatedSize, rotation);
        context.Preview.SetValid(canPlace);
    }

   

    // 予約段階
    public bool TryCreatePlacementRequest(
        out Vector3Int originCell,
        out ObjectData data,
        out Quaternion rotation,
        bool debugLogsOverride = false)
    {
        originCell = default;
        data = null;
        rotation = Quaternion.identity;

        if (state.PlaceTool == BuildTool.Move)
        {
            Log("[BuildPlacementService] TryCreatePlacementRequest: Move tool cannot create placement request.", debugLogsOverride);
            return false;
        }

        if (!TryGetSelectedData(out data))
        {
            Log("[BuildPlacementService] TryCreatePlacementRequest: selected data not found.", debugLogsOverride);
            return false;
        }

        rotation = state.CurrentRotation;
        Vector3Int rotatedSize = context.Solver.GetRotatedSize(data.SizeXYZ, rotation);

        if (!context.Solver.TryGetHoverCell(
                context.Raycaster,
                rotatedSize,
                out Vector3Int hoverCell,
                out _))
        {
            Log("[BuildPlacementService] TryCreatePlacementRequest: hover cell not found.", debugLogsOverride);
            return false;
        }

        if (state.PlaceTool == BuildTool.Single)
        {
            if (!context.Rules.CanPlaceObject(
                    data,
                    context.Database,
                    hoverCell,
                    rotatedSize,
                    out string reason))
            {
                Log($"[BuildPlacementService] TryCreatePlacementRequest denied: {reason}", debugLogsOverride);
                return false;
            }

            originCell = hoverCell;
            return true;
        }

        if (state.PlaceTool == BuildTool.Line)
        {
            if (!context.Rules.CanUseLineTool(data, out string reason))
            {
                Log($"[BuildPlacementService] TryCreatePlacementRequest line denied: {reason}", debugLogsOverride);
                return false;
            }

            // 1回目クリック: ライン開始点だけ予約するのではなく、今まで通り開始だけして終了
            if (!state.HasLineStart)
            {
                state.BeginLine(hoverCell);
                Log("[BuildPlacementService] TryCreatePlacementRequest: line start selected.", debugLogsOverride);
                return false;
            }

            if (!context.Solver.TryGetLineCellsOrthogonal(
                    state.LineStartCell,
                    hoverCell,
                    rotatedSize,
                    out List<Vector3Int> lineCells) ||
                lineCells == null ||
                lineCells.Count == 0)
            {
                Log("[BuildPlacementService] TryCreatePlacementRequest: line cells not found.", debugLogsOverride);
                state.CancelLine();
                return false;
            }

            // BuildApplicationService が単一セル前提なので、
            // ここでは line の開始セルを返すだけにせず、
            // line は TryPlaceReserved 側で再計算するため hoverCell を返しておく
            originCell = hoverCell;
            return true;
        }

        Log("[BuildPlacementService] TryCreatePlacementRequest: unsupported tool.", debugLogsOverride);
        return false;
    }

    // 確定段階
    public bool TryPlaceReserved(
        Vector3Int originCell,
        ObjectData data,
        Quaternion rotation,
        bool debugLogsOverride = false)
    {
        if (data == null)
        {
            Log("[BuildPlacementService] TryPlaceReserved: data is null.", debugLogsOverride);
            return false;
        }

        if (state.PlaceTool == BuildTool.Single)
            return TryPlaceSingle(data, originCell, rotation, debugLogsOverride);

        if (state.PlaceTool == BuildTool.Line)
            return TryPlaceLineReserved(data, originCell, rotation, debugLogsOverride);

        Log("[BuildPlacementService] TryPlaceReserved: unsupported tool.", debugLogsOverride);
        return false;
    }

    private bool TryPlaceSingle(
        ObjectData data,
        Vector3Int hoverCell,
        Quaternion rotation,
        bool debugLogsOverride)
    {
        PlaceCommand cmd = new PlaceCommand(context, hoverCell, data, rotation);
        return context.History.Do(cmd, debugLogsOverride);
    }

    private bool TryPlaceLineReserved(
        ObjectData data,
        Vector3Int hoverCell,
        Quaternion rotation,
        bool debugLogsOverride)
    {
        Vector3Int rotatedSize = context.Solver.GetRotatedSize(data.SizeXYZ, rotation);

        if (!context.Rules.CanUseLineTool(data, out string reason))
        {
            Log($"[BuildPlacementService] TryPlaceLineReserved denied: {reason}", debugLogsOverride);
            return false;
        }

        if (!state.HasLineStart)
        {
            Log("[BuildPlacementService] TryPlaceLineReserved: line start missing.", debugLogsOverride);
            return false;
        }

        if (!context.Solver.TryGetLineCellsOrthogonal(
                state.LineStartCell,
                hoverCell,
                rotatedSize,
                out List<Vector3Int> lineCells) ||
            lineCells == null ||
            lineCells.Count == 0)
        {
            state.CancelLine();
            Log("[BuildPlacementService] TryPlaceLineReserved: line cells invalid.", debugLogsOverride);
            return false;
        }

        CompositeCommand composite = new CompositeCommand("Place Line");

        for (int i = 0; i < lineCells.Count; i++)
        {
            if (!context.Rules.CanPlaceObject(
                    data,
                    context.Database,
                    lineCells[i],
                    rotatedSize,
                    out _))
            {
                state.CancelLine();
                Log($"[BuildPlacementService] TryPlaceLineReserved: cannot place at {lineCells[i]}.", debugLogsOverride);
                return false;
            }

            composite.Add(new PlaceCommand(context, lineCells[i], data, rotation));
        }

        bool ok = context.History.Do(composite, debugLogsOverride);
        state.CancelLine();
        return ok;
    }

    private void UpdateLinePreview(ObjectData data, Quaternion rotation, Vector3Int rotatedSize, Vector3Int hoverCell)
    {
        if (!context.Solver.TryGetLineCellsOrthogonal(
                state.LineStartCell,
                hoverCell,
                rotatedSize,
                out List<Vector3Int> lineCells) ||
            lineCells == null ||
            lineCells.Count == 0)
        {
            context.Preview.Clear();
            if (context.Drone != null)
                context.Drone.SetIdle();
            return;
        }

        context.Preview.ShowLine(lineCells, rotatedSize, rotation);

        bool allValid = true;
        for (int i = 0; i < lineCells.Count; i++)
        {
            if (!context.Rules.CanPlaceObject(data, context.Database, lineCells[i], rotatedSize, out _))
            {
                allValid = false;
                break;
            }
        }

        context.Preview.SetValid(allValid);
    }

    private void UpdateMovePreview()
    {
        if (!state.HasMoveTarget || state.MoveTarget == null)
        {
            context.Preview.Clear();

            if (context.Drone != null)
                context.Drone.SetIdle();
            return;
        }

        BlockInstance moveTarget = state.MoveTarget;

        if (!context.Raycaster.RaycastForBlock(out RaycastHit hit))
        {
            context.Preview.Clear();
            if (context.Drone != null)
                context.Drone.SetIdle();
            return;
        }

        if (!context.Solver.TrySolveOriginCell(hit, moveTarget.SizeXYZ, out Vector3Int toCell))
        {
            context.Preview.Clear();
            if (context.Drone != null)
                context.Drone.SetIdle();
            return;
        }

        bool canPlace = context.Rules.CanPlaceIgnoring(moveTarget, toCell, moveTarget.SizeXYZ, out _);

        Vector3 worldPos = context.Grid.BoxToWorldCenter(toCell, moveTarget.SizeXYZ);
        context.Preview.ShowMovePreview(moveTarget.gameObject, worldPos);
        context.Preview.SetValid(canPlace);
    }

    private bool TryGetSelectedData(out ObjectData data)
    {
        if (context.Database == null)
        {
            data = null;
            return false;
        }

        return context.Database.TryGetByID(state.SelectedObjectID, out data)
            && data != null
            && data.Prefab != null;
    }

    private void HidePreviewAndIdleDrone()
    {
        context.Preview.Clear();

        if (context.Drone != null)
            context.Drone.SetIdle();
    }

    private void Log(string msg, bool enabled)
    {
        if (enabled && !string.IsNullOrEmpty(msg))
            Debug.Log(msg);
    }
}