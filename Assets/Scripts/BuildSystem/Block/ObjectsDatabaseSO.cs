using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Build/Objects Database")]
public class ObjectsDatabaseSO : ScriptableObject
{
    [SerializeField] private List<ObjectData> objectsData = new();

    public IReadOnlyList<ObjectData> ObjectsData => objectsData;

    public bool TryGetByID(int id, out ObjectData data)
    {
        for (int i = 0; i < objectsData.Count; i++)
        {
            if (objectsData[i] != null && objectsData[i].ID == id)
            {
                data = objectsData[i];
                return true;
            }
        }

        data = null;
        return false;
    }

    public List<ObjectData> GetByCategory(ObjectCategory category)
    {
        List<ObjectData> result = new List<ObjectData>();

        for (int i = 0; i < objectsData.Count; i++)
        {
            ObjectData obj = objectsData[i];
            if (obj == null)
                continue;

            if (obj.Category == category)
                result.Add(obj);
        }

        return result;
    }
}

[Serializable]
public class ObjectData
{
   
    [field: SerializeField] public string Name { get; private set; }
    [field: SerializeField] public int ID { get; private set; }

    // X=width, Y=height, Z=depth (cells)
    [field: SerializeField] public Vector3Int SizeXYZ { get; private set; } = Vector3Int.one;

    [field: SerializeField] public bool IsBoxShape { get; private set; }

    [field: SerializeField] public ObjectCategory Category { get; private set; } = ObjectCategory.None;

    // Start / Goal ”»’è—p
    [field: SerializeField] public SpecialBlockType SpecialType { get; private set; } = SpecialBlockType.None;

    [field: SerializeField] public GameObject Prefab { get; private set; }

    [Header("Color Variants")]
    [SerializeField] private GameObject bluePrefab;
    [SerializeField] private GameObject redPrefab;
    [SerializeField] private GameObject yellowPrefab;
    [SerializeField] private GameObject greenPrefab;

    public GameObject GetPrefab(BlockColor color)
    {
        switch (color)
        {
            case BlockColor.Blue:
                return bluePrefab != null ? bluePrefab : GetFallbackPrefab();

            case BlockColor.Red:
                return redPrefab != null ? redPrefab : GetFallbackPrefab();

            case BlockColor.Yellow:
                return yellowPrefab != null ? yellowPrefab : GetFallbackPrefab();

            case BlockColor.Green:
                return greenPrefab != null ? greenPrefab : GetFallbackPrefab();

            default:
                return GetFallbackPrefab();
        }
    }

    public bool HasColorVariant(BlockColor color)
    {
        return GetPrefab(color) != null;
    }

    private GameObject GetFallbackPrefab()
    {
        if (Prefab != null) return Prefab;
        if (bluePrefab != null) return bluePrefab;
        if (redPrefab != null) return redPrefab;
        if (yellowPrefab != null) return yellowPrefab;
        if (greenPrefab != null) return greenPrefab;
        return null;
    }
    public bool HasExactColorVariant(BlockColor color)
    {
        switch (color)
        {
            case BlockColor.Blue:
                return bluePrefab != null;

            case BlockColor.Red:
                return redPrefab != null;

            case BlockColor.Yellow:
                return yellowPrefab != null;

            case BlockColor.Green:
                return greenPrefab != null;

            default:
                return false;
        }
    }
    public bool HasAnyExactColorVariant()
    {
        return bluePrefab != null ||
               redPrefab != null ||
               yellowPrefab != null ||
               greenPrefab != null;
    }
}