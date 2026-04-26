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
    }
}
