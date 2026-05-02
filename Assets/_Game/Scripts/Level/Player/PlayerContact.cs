using UnityEngine;

public class PlayerContact : MonoBehaviour
{
    [SerializeField] private PlayerCollector _playerCollector;

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Finish"))
        {
            Debug.Log("Player reached the finish!");
            Actions.PlayerReachedFinish();
        }
        if (other.CompareTag("Lift"))
        {
            Lift lift = other.GetComponent<Lift>();
            if (lift != null && !lift.IsMoving())
            {
                Debug.Log("Player entered the lift area!");
                if (lift.IsAtTop()) lift.MoveDown();
                if (!lift.IsAtTop()) lift.MoveUp();
            }
        }
        if (other.CompareTag("LiftTrigger"))
        {
            LiftTrigger liftTrigger = other.GetComponent<LiftTrigger>();
            if (liftTrigger != null)
            {
                Debug.Log("Player activated a lift trigger!");
                liftTrigger.CallTheLift();
            }
        }
        if (other.CompareTag("Contact"))
        {
            BottleReturnMachine bottleReturnMachine = other.GetComponent<BottleReturnMachine>();
            if (bottleReturnMachine != null)
            {
                bottleReturnMachine.StartReturning(_playerCollector);
            }
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
