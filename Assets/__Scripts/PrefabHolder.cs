using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PrefabHolder : MonoBehaviour
{
    [SerializeField] private GameObject prefab;
    public void SpawnPrefab(Vector3 spawnPosition, Quaternion spawnRotation)
    {
        Instantiate(prefab, spawnPosition, spawnRotation);
    }

    public void SpawnNetworkPrefab(Vector3 spawnPosition, Quaternion spawnRotation)
    {
        Game.Manager.RequestSpawnServerRpc(prefab, spawnPosition, spawnRotation);
    }
}
