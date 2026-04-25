using UnityEngine;
using System;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "LevelsList", menuName = "Scriptable Objects/LevelsList")]
public class LevelsList : ScriptableObject
{
    [SerializeField] List<LevelData> levels;
}

[Serializable]
public class LevelData
{
    public string levelName;
    public GameObject levelPrefab;
}