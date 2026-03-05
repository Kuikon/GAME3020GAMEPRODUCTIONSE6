using UnityEngine;

public class MoveCommand : IBuildCommand
{
    public string Name { get; }
    private readonly GridManager grid;
    private readonly BuildSpawner spawner;    
    private readonly BuildPlacementRules rules;
    public MoveCommand(
       GridManager grid,
       BuildSpawner spawner,
       BuildPlacementRules rules,
       BlockInstance target,
       Vector3Int toCell,
       string name = "Move")
    {
        this.grid = grid;
        this.spawner = spawner;
        this.rules = rules;

        this.target = target;
        this.fromCell = target != null ? target.OriginCell : default;
        this.toCell = toCell;
        this.size = target != null ? target.SizeXYZ : Vector3Int.one;
        this.rot = target != null ? target.Rotation : Quaternion.identity;

        Name = name;
    }

    private readonly BlockInstance target;
    private readonly Vector3Int fromCell;
    private readonly Vector3Int toCell;
    private readonly Vector3Int size;
    private readonly Quaternion rot;
    public bool Execute()
    {
        if (grid == null || spawner == null || rules == null) return false;
        if (target == null) return false;
        if (fromCell == toCell) return false;

        // Moveの判定は「自分自身を無視」してチェック
        if (!rules.CanPlaceIgnoring(target, toCell, size, out _))
            return false;

        // ① 今いるセルの占有を消す
        rules.RemoveObjectCells(fromCell, size);

        // ② 見た目(Transform)を移動
        spawner.MoveExisting(grid, target, toCell, size, rot);

        // ③ BlockInstanceの情報を更新
        target.Setup(target.ObjectID, toCell, size, rot);

        // ④ 新しいセルを占有として登録
        rules.RegisterObjectCells(toCell, size, target);

        return true;
    }
    public void Undo()
    {
        if (grid == null || spawner == null || rules == null) return;
        if (target == null) return;

        // ① 今いる（移動後）セルの占有を消す
        rules.RemoveObjectCells(toCell, size);

        // ② 見た目を元に戻す
        spawner.MoveExisting(grid, target, fromCell, size, rot);

        // ③ BlockInstance情報を元に戻す
        target.Setup(target.ObjectID, fromCell, size, rot);

        // ④ 元のセルを占有として登録し直す
        rules.RegisterObjectCells(fromCell, size, target);
    }
}
