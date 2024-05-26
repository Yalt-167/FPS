using System.Collections;
using System.Collections.Generic;
using UnityEngine;
// perhaps change that so the spawnpoints are "assigned to a main unit that holds the team to avoid having to set each one manually
public class SpawnPoint : MonoBehaviour
{
    public ushort TeamID;
    public bool Active;
    public Vector3 SpawnPosition => transform.position + Vector3.up;

    private void Awake()
    {
        Game.Manager.AddRespawnPoint(this);
    }

    private void OnDisable()
    {
        Game.Manager.DiscardRespawnPoint(this);
    }
}
