using UnityEngine;

public class QuestDestinationMarker : MonoBehaviour
{
    string _questId;

    public void Init(string questId)
    {
        _questId = questId;
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.attachedRigidbody != null && other.attachedRigidbody.CompareTag("Player"))
        {
            Actions.QuestDestinationReached(_questId);
        }
    }
}
