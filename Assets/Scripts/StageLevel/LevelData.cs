using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class LevelIndex
{
    public List<LevelMeta> levels = new();
}

[Serializable]
public class LevelMeta
{
    public string levelId;
    public string name;
    public long updatedAtTicks;
    public string thumbnailPath;
}

[Serializable]
public class LevelData
{
    public string levelId;
    public List<BlockRecord> blocks = new();
}

[Serializable]
public class BlockRecord
{
    public int objectId;
    public Vector3Int originCell;
    public Vector3 euler; 
}