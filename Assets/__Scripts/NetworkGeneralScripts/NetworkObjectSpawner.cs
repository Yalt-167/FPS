using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class NetworkObjectSpawner : NetworkBehaviour
{
    [SerializeField]
    private GameObject networkObjectPrefab;

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            SpawnNetworkObject();
        }
    }

    private void SpawnNetworkObject()
    {
        // Ensure the prefab is registered with the NetworkManager
        if (networkObjectPrefab != null && networkObjectPrefab.GetComponent<NetworkObject>() != null)
        {
            GameObject obj = Instantiate(networkObjectPrefab, Vector3.zero, Quaternion.identity);
            NetworkObject networkObject = obj.GetComponent<NetworkObject>();
            networkObject.Spawn();
        }
        else
        {
            Debug.LogError("Prefab is not properly set up or is missing a NetworkObject component.");
        }
    }
}
