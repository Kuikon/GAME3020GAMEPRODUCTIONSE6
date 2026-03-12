using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Build/Objects Database")]
public class ObjectsDatabaseSO : ScriptableObject
{
    public List<ObjectData> objectsData = new();

    public IReadOnlyList<ObjectData> Objects => objectsData;

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
            if (obj == null) continue;

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

    [field: SerializeField] public GameObject Prefab { get; private set; }
    [field: SerializeField] public bool IsBoxShape { get; private set; }
    [field: SerializeField] public ObjectCategory Category = ObjectCategory.None;
}
