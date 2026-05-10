using UnityEngine;

public class PlayerContact : MonoBehaviour
{
    [SerializeField] private PlayerCollector _playerCollector;

    void OnTriggerEnter(Collider other)
    {
        switch (other.tag)
        {
            case "Finish":
                HandleFinish();
                break;
            case "Lift":
                HandleLift(other);
                break;
            case "LiftTrigger":
                HandleLiftTrigger(other);
                break;
            case "Contact":
                HandleContactEnter(other);
                break;
            case "QuestDestinationMarker":
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
        Lift lift = other.GetComponent<Lift>();
        if (lift != null && !lift.IsMoving())
        {
            Debug.Log("Player entered the lift area!");
            if (lift.IsAtTop()) lift.MoveDown();
            else lift.MoveUp();
        }
    }

    void HandleLiftTrigger(Collider other)
    {
        LiftTrigger liftTrigger = other.GetComponent<LiftTrigger>();
        if (liftTrigger != null)
        {
            Debug.Log("Player activated a lift trigger!");
            liftTrigger.CallTheLift();
        }
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
        if (other.CompareTag("Contact"))
        {
            BottleReturnMachine bottleReturnMachine = other.GetComponent<BottleReturnMachine>();
            if (bottleReturnMachine != null)
            {
                bottleReturnMachine.StopReturning();
            }
        }
    }
}
