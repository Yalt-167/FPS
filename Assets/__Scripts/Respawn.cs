using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Respawn : MonoBehaviour
{
    public Transform respawnPoint;

    public LayerMask limits;
    private bool offLimits;
    void Update()
    {
        offLimits = Physics.Raycast(transform.position, Vector3.down, 2f * 0.5f + 0.2f, limits);
        
        if (offLimits)
            RespawnPlayer();
        
        
    }

    private void RespawnPlayer()
    {

        transform.position = respawnPoint.position;
        transform.rotation = respawnPoint.rotation;

    }
}
