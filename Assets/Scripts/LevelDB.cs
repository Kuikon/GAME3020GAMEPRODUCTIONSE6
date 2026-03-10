using System;
using System.IO;
using UnityEngine;

public class LevelDB
{
    private readonly string levelsDir;
    private readonly string thumbsDir;
    private readonly string indexPath;

    public LevelDB()
    {
        levelsDir = Path.Combine(Application.persistentDataPath, "levels");
        thumbsDir = Path.Combine(levelsDir, "thumbs");
        indexPath = Path.Combine(levelsDir, "index.json");

        Directory.CreateDirectory(levelsDir);
        Directory.CreateDirectory(thumbsDir);
    }

    // -------- Index (Meta list) --------
    public LevelIndex LoadIndex()
    {
        if (!File.Exists(indexPath))
            return new LevelIndex();

        string json = File.ReadAllText(indexPath);
        return JsonUtility.FromJson<LevelIndex>(json) ?? new LevelIndex();
    }

    public void SaveIndex(LevelIndex index)
    {
        string json = JsonUtility.ToJson(index, true);
        File.WriteAllText(indexPath, json);
    }

    // -------- Level Data --------
    public LevelData LoadLevel(string levelId)
    {
        string path = LevelPath(levelId);
        if (!File.Exists(path))
            return new LevelData { levelId = levelId };

        string json = File.ReadAllText(path);
        return JsonUtility.FromJson<LevelData>(json) ?? new LevelData { levelId = levelId };
    }

    public void SaveLevel(LevelData data)
    {
        if (data == null || string.IsNullOrEmpty(data.levelId)) return;

        string json = JsonUtility.ToJson(data, true);
        File.WriteAllText(LevelPath(data.levelId), json);

        TouchUpdatedAt(data.levelId);
    }

    // -------- CRUD for Meta --------
    public LevelMeta CreateNew(string name)
    {
        var index = LoadIndex();

        string id = Guid.NewGuid().ToString("N");
        var meta = new LevelMeta
        {
            levelId = id,
            name = string.IsNullOrWhiteSpace(name) ? "New Level" : name,
            updatedAtTicks = DateTime.UtcNow.Ticks,
            thumbnailPath = ""
        };

        // 空レベルを作って保存
        SaveLevel(new LevelData { levelId = id });

        index.levels.Insert(0, meta);
        SaveIndex(index);

        return meta;
    }

    public void Rename(string levelId, string newName)
    {
        var index = LoadIndex();
        var meta = index.levels.Find(x => x.levelId == levelId);
        if (meta == null) return;

        if (!string.IsNullOrWhiteSpace(newName))
            meta.name = newName;

        meta.updatedAtTicks = DateTime.UtcNow.Ticks;
        SaveIndex(index);
    }

    public LevelMeta Duplicate(string sourceLevelId)
    {
        var srcData = LoadLevel(sourceLevelId);

        var newMeta = CreateNew("Copy");
        // 同じblocksを持ったままIDだけ差し替え
        srcData.levelId = newMeta.levelId;
        SaveLevel(srcData);

        // 名前をコピーっぽく
        Rename(newMeta.levelId, $"{GetName(sourceLevelId)} (Copy)");

        return newMeta;
    }

    public void Delete(string levelId)
    {
        var index = LoadIndex();
        index.levels.RemoveAll(x => x.levelId == levelId);
        SaveIndex(index);

        string p = LevelPath(levelId);
        if (File.Exists(p)) File.Delete(p);

        string t = ThumbPath(levelId);
        if (File.Exists(t)) File.Delete(t);
    }

    // -------- helpers --------
    private string LevelPath(string levelId) => Path.Combine(levelsDir, $"{levelId}.json");
    private string ThumbPath(string levelId) => Path.Combine(thumbsDir, $"{levelId}.png");

    private void TouchUpdatedAt(string levelId)
    {
        var index = LoadIndex();
        var meta = index.levels.Find(x => x.levelId == levelId);
        if (meta == null) return;

        meta.updatedAtTicks = DateTime.UtcNow.Ticks;
        SaveIndex(index);
    }

    private string GetName(string levelId)
    {
        var index = LoadIndex();
        return index.levels.Find(x => x.levelId == levelId)?.name ?? "Level";
    }
    public string GetThumbnailPath(string levelId)
    {
        return ThumbPath(levelId);
    }

    public void SetThumbnailPath(string levelId, string thumbnailPath)
    {
        var index = LoadIndex();
        var meta = index.levels.Find(x => x.levelId == levelId);
        if (meta == null) return;

        meta.thumbnailPath = thumbnailPath;
        meta.updatedAtTicks = DateTime.UtcNow.Ticks;
        SaveIndex(index);
    }
}