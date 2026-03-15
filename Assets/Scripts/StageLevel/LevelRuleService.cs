using UnityEngine;

public class LevelRuleService
{
    private readonly ObjectsDatabaseSO database;

    public LevelRuleService(ObjectsDatabaseSO database)
    {
        this.database = database;
    }

    public BlockInstance GetStartBlock()
    {
        return GetFirstBlockByCategory(ObjectCategory.Start);
    }

    public BlockInstance GetGoalBlock()
    {
        return GetFirstBlockByCategory(ObjectCategory.Goal);
    }

    public bool HasStartBlock()
    {
        return GetStartBlock() != null;
    }

    public bool HasGoalBlock()
    {
        return GetGoalBlock() != null;
    }

    public bool CanEnterPlay()
    {
        return HasStartBlock() && HasGoalBlock();
    }

    private BlockInstance GetFirstBlockByCategory(ObjectCategory category)
    {
        if (database == null)
            return null;

        BlockInstance[] blocks = Object.FindObjectsByType<BlockInstance>(FindObjectsSortMode.None);

        foreach (BlockInstance block in blocks)
        {
            if (block == null)
                continue;

            if (!database.TryGetByID(block.ObjectID, out ObjectData data) || data == null)
                continue;

            if (data.Category == category)
                return block;
        }

        return null;
    }
}