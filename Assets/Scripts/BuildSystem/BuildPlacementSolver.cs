// BuildPlacementSolver.cs (adds diagonal line + optional orthogonal line)
using System.Collections.Generic;
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
    public bool TryGetLineCellsOrthogonal(Vector3Int startCell, Vector3Int endCell, Vector3Int placeSize, out List<Vector3Int> cells)
    {
        cells = null;
        if (grid == null) return false;

        // dominant axis lock
        int dxAbs = Mathf.Abs(endCell.x - startCell.x);
        int dzAbs = Mathf.Abs(endCell.z - startCell.z);
        if (dxAbs >= dzAbs) endCell.z = startCell.z;
        else endCell.x = startCell.x;

        int dx = endCell.x - startCell.x;
        int dz = endCell.z - startCell.z;

        Vector3Int step;
        int steps;

        if (dx != 0)
        {
            int dir = dx > 0 ? 1 : -1;
            int stride = Mathf.Max(1, placeSize.x);
            step = new Vector3Int(dir * stride, 0, 0);
            steps = Mathf.Abs(dx) / stride;
        }
        else
        {
            int dir = dz > 0 ? 1 : -1;
            int stride = Mathf.Max(1, placeSize.z);
            step = new Vector3Int(0, 0, dir * stride);
            steps = Mathf.Abs(dz) / stride;
        }

        cells = new List<Vector3Int>(steps + 1);
        var c = startCell;
        for (int i = 0; i <= steps; i++)
        {
            cells.Add(c);
            c += step;
        }

        return cells.Count > 0;
    }

    private List<Vector3Int> BresenhamXZ(Vector3Int a, Vector3Int b)
    {
        int x0 = a.x, z0 = a.z;
        int x1 = b.x, z1 = b.z;

        int dx = Mathf.Abs(x1 - x0);
        int dz = Mathf.Abs(z1 - z0);

        int sx = (x0 < x1) ? 1 : -1;
        int sz = (z0 < z1) ? 1 : -1;

        int err = dx - dz;

        var result = new List<Vector3Int>(dx + dz + 1);

        while (true)
        {
            result.Add(new Vector3Int(x0, groundYCell, z0));

            if (x0 == x1 && z0 == z1) break;

            int e2 = 2 * err;

            if (e2 > -dz)
            {
                err -= dz;
                x0 += sx;
            }

            if (e2 < dx)
            {
                err += dx;
                z0 += sz;
            }
        }

        return result;
    }

    // -------------------------
    // Internals
    // -------------------------
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

        Vector3Int inside = grid.WorldToCell(hitPoint - (Vector3)faceNormal * 0.01f);

        inside.x = Mathf.Clamp(inside.x, tMinX, tMaxX - 1);
        inside.y = Mathf.Clamp(inside.y, tMinY, tMaxY - 1);
        inside.z = Mathf.Clamp(inside.z, tMinZ, tMaxZ - 1);

        int ClampOrigin(int v, int minInclusive, int maxExclusive, int size)
        {
            // origin ‚ÌÅ‘å’l‚Í (maxExclusive - size)
            int maxOrigin = maxExclusive - Mathf.Max(1, size);
            return Mathf.Clamp(v, minInclusive, maxOrigin);
        }

        int ox = inside.x;
        int oy = inside.y;
        int oz = inside.z;
        if (faceNormal.x > 0) ox = tMaxX;               
        else if (faceNormal.x < 0) ox = tMinX - placeSize.x;

        if (faceNormal.y > 0) oy = tMaxY;               
        else if (faceNormal.y < 0) oy = tMinY - placeSize.y;

        if (faceNormal.z > 0) oz = tMaxZ;               
        else if (faceNormal.z < 0) oz = tMinZ - placeSize.z;

        if (faceNormal.y != 0)
        {
            ox = ClampOrigin(ox, tMinX, tMaxX, placeSize.x);
            oz = ClampOrigin(oz, tMinZ, tMaxZ, placeSize.z);
        }
        else if (faceNormal.x != 0)
        {
            oy = ClampOrigin(oy, tMinY, tMaxY, placeSize.y);
            oz = ClampOrigin(oz, tMinZ, tMaxZ, placeSize.z);
        }
        else // faceNormal.z != 0
        {
            ox = ClampOrigin(ox, tMinX, tMaxX, placeSize.x);
            oy = ClampOrigin(oy, tMinY, tMaxY, placeSize.y);
        }

        return new Vector3Int(ox, oy, oz);
    }
}