using UnityEngine;

public enum CollectableType
{
    Money = 0
}

public class Collectable : MonoBehaviour
{
    public CollectableType type;
    public int value = 1;

}



