using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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
