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
        if (Physics.Raycast(transform.position, Vector3.down, 2f * 0.5f + 0.2f, limits)) { RespawnPlayer(); }     
    }

    private void RespawnPlayer()
    {
        transform.SetPositionAndRotation(respawnPoint.position, respawnPoint.rotation);
    }
}
