using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public sealed class NetworkObjectSpawner : NetworkBehaviour
{
    [SerializeField] private GameObject networkObjectPrefab;

    public void SpawnNetworkObject()
    {
        // ensure the prefab is registered on the NetworkManager
        if (networkObjectPrefab == null)
        {
            Debug.Log("This NetworkObjectSpawner doesn t have an object to spawn", gameObject);
            return;
        }

        if (!networkObjectPrefab.TryGetComponent<NetworkObject>(out var _))
        {
            Debug.Log("this NetworkObjectSpawner s prefab does not have a NetworkObject component", gameObject);
            return;
        }
       
        Instantiate(networkObjectPrefab, Vector3.zero, Quaternion.identity).GetComponent<NetworkObject>().Spawn();
    }
}
