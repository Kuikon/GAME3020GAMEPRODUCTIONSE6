using UnityEngine;

public class BuildPlacementSolver
{
    private readonly GridManager grid;
    private readonly int groundYCell;

    public BuildPlacementSolver(GridManager grid, int groundYCell)
    {
        this.grid = grid;
        this.groundYCell = groundYCell;
    }

    public bool TrySolveOriginCell(RaycastHit hit, Vector3Int placeSize, out Vector3Int originCell)
    {
        originCell = default;
        if (grid == null) return false;

        Vector3Int n = NormalToInt(hit.normal);

        if (TryGetBlockInstance(hit, out var target))
        {
            originCell = GetSnappedOriginNextToBox(target.OriginCell, target.SizeXYZ, placeSize, n, hit.point);
            return true;
        }

        Vector3Int cell = grid.WorldToCell(hit.point);
        cell.y = groundYCell;
        originCell = cell;
        return true;
    }

    public bool TrySolveRemoveCell(RaycastHit hit, out Vector3Int cell)
    {
        cell = default;
        if (grid == null) return false;

        Vector3 inside = hit.point - hit.normal * 0.01f;
        cell = grid.WorldToCell(inside);
        return true;
    }

    private bool TryGetBlockInstance(RaycastHit hit, out BlockInstance bi)
    {
        bi = hit.collider.GetComponentInParent<BlockInstance>();
        return bi != null;
    }

    private static Vector3Int NormalToInt(Vector3 n)
    {
        n = n.normalized;

        float ax = Mathf.Abs(n.x);
        float ay = Mathf.Abs(n.y);
        float az = Mathf.Abs(n.z);

        if (ax >= ay && ax >= az) return new Vector3Int((int)Mathf.Sign(n.x), 0, 0);
        if (ay >= ax && ay >= az) return new Vector3Int(0, (int)Mathf.Sign(n.y), 0);
        return new Vector3Int(0, 0, (int)Mathf.Sign(n.z));
    }

    private Vector3Int GetSnappedOriginNextToBox(
        Vector3Int targetOrigin,
        Vector3Int targetSize,
        Vector3Int placeSize,
        Vector3Int faceNormal,
        Vector3 hitPoint)
    {
        int tMinX = targetOrigin.x;
        int tMinY = targetOrigin.y;
        int tMinZ = targetOrigin.z;

        int tMaxX = targetOrigin.x + targetSize.x;
        int tMaxY = targetOrigin.y + targetSize.y;
        int tMaxZ = targetOrigin.z + targetSize.z;

        Vector3Int insideCell = grid.WorldToCell(hitPoint - (Vector3)faceNormal * 0.01f);

        int ox = insideCell.x;
        int oy = insideCell.y;
        int oz = insideCell.z;

        if (faceNormal.x > 0) ox = tMaxX;
        else if (faceNormal.x < 0) ox = tMinX - placeSize.x;

        if (faceNormal.y > 0) oy = tMaxY;
        else if (faceNormal.y < 0) oy = tMinY - placeSize.y;

        if (faceNormal.z > 0) oz = tMaxZ;
        else if (faceNormal.z < 0) oz = tMinZ - placeSize.z;

        return new Vector3Int(ox, oy, oz);
    }
}