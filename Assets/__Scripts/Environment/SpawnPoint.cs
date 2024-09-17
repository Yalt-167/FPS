using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using GameManagement;

public sealed class SpawnPoint : MonoBehaviour
{
    public ushort TeamNumber;
    public bool Active;
    public Vector3 SpawnPosition { get; private set; }

    private void Awake()
    {
        Game.Manager.AddRespawnPoint(this);
        SpawnPosition = transform.position + Vector3.up;
    }

    private void OnDisable()
    {
        Game.Manager.DiscardRespawnPoint(this);
    }
}
