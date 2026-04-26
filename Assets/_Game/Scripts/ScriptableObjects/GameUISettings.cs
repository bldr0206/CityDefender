using UnityEngine;

[CreateAssetMenu(fileName = "GameUISettings", menuName = "Scriptable Objects/GameUISettings")]
public class GameUISettings : ScriptableObject
{
    public float standardDelay = 1f;
    public float shortDelay = 0.5f;
    public float minimalDelay = 0.125f;
}
