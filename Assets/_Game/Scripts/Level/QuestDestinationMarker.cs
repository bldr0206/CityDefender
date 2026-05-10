using UnityEngine;

public class QuestDestinationMarker : MonoBehaviour
{
    string _questId;

    public void Init(string questId)
    {
        _questId = questId;
    }



    public void Reached()
    {
        Actions.QuestDestinationReached(_questId);
    }
}
