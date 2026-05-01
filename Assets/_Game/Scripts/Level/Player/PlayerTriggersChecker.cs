using System;
using UnityEngine;

public class PlayerTriggersChecker : MonoBehaviour
{

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
    }
}
