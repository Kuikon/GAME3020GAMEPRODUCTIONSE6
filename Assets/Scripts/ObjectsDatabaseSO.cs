using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Build/Objects Database")]
public class ObjectsDatabaseSO : ScriptableObject
{
    public List<ObjectData> objectsData = new();

    public bool TryGetByID(int id, out ObjectData data)
    {
        data = objectsData.Find(o => o.ID == id);
        return data != null;
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
}
