using UnityEngine;

[System.Serializable]
public class BreakableDropEntry
{
    public GameObject prefab;
    [Min(0)] public int count = 1;

    public int GetSpawnCount()
    {
        return Mathf.Max(0, count);
    }
}
