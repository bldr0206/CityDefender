using UnityEngine;

public class LiftTrigger : MonoBehaviour
{
    public bool isUpTrigger;
    public Lift lift;

    public void CallTheLift()
    {
        if (isUpTrigger)
        {
            lift.MoveUp();
        }
        else
        {
            lift.MoveDown();
        }
    }
}

