using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Goal : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        //SaveAndLoad.Save(Game.Manager.GameTime);
        Game.Manager.NextLevel();
    }
}
