using System.Collections;
using System.Collections.Generic;

using UnityEngine;

using GameManagement;

public sealed class PrefabHolder : MonoBehaviour
{
    [SerializeField] private GameObject prefab;
    public void SpawnPrefab(Vector3 spawnPosition, Quaternion spawnRotation)
    {
        Instantiate(prefab, spawnPosition, spawnRotation);
    }

    public void SpawnNetworkPrefab(Vector3 spawnPosition, Quaternion spawnRotation)
    {
        Game.Manager.RequestNetworkObjectClientSpawnServerRpc(prefab, spawnPosition, spawnRotation);
    }
}
