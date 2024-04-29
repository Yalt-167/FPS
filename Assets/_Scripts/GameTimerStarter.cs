using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameTimerStarter : MonoBehaviour
{
    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            Game.Manager.StartTimer();
        }
    }
}
