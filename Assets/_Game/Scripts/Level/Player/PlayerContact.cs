using UnityEngine;

public class PlayerContact : MonoBehaviour
{
    [SerializeField] private PlayerCollector _playerCollector;

    void OnTriggerEnter(Collider other)
    {
        switch (other.tag)
        {
            case GameTags.Finish:
                HandleFinish();
                break;
            case GameTags.Lift:
                HandleLift(other);
                break;
            case GameTags.LiftTrigger:
                HandleLiftTrigger(other);
                break;
            case GameTags.Contact:
                HandleContactEnter(other);
                break;
            case GameTags.QuestDestinationMarker:
                HandleQuestDestinationMarker(other);
                break;
        }
    }

    void HandleFinish()
    {
        Debug.Log("Player reached the finish!");
        Actions.PlayerReachedFinish();
    }

    void HandleLift(Collider other)
    {
        // Тег Lift — объём кабины; поездка не от него, а от кнопки <see cref="Lift.MoveToOppositeFloor"/> или зон <see cref="LiftTrigger"/>.
    }

    void HandleLiftTrigger(Collider other)
    {
        LiftTrigger liftTrigger = other.GetComponent<LiftTrigger>();
        if (liftTrigger != null)
            liftTrigger.CallTheLift();
    }

    void HandleContactEnter(Collider other)
    {
        BottleReturnMachine bottleReturnMachine = other.GetComponent<BottleReturnMachine>();
        if (bottleReturnMachine != null)
        {
            bottleReturnMachine.StartReturning(_playerCollector);
        }
    }

    void HandleQuestDestinationMarker(Collider other)
    {
        QuestDestinationMarker questDestinationMarker = other.GetComponent<QuestDestinationMarker>();
        if (questDestinationMarker != null)
        {
            questDestinationMarker.Reached();
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag(GameTags.Contact))
        {
            BottleReturnMachine bottleReturnMachine = other.GetComponent<BottleReturnMachine>();
            if (bottleReturnMachine != null)
            {
                bottleReturnMachine.StopReturning();
            }
        }
    }
}
