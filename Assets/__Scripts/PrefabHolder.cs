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
}
