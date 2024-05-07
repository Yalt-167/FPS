using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Burst.CompilerServices;
using Unity.Netcode;
using UnityEngine;

[DefaultExecutionOrder(-5)]
public class PlayerCombatNetworked : NetworkBehaviour
{

    [SerializeField] private GameObject landingShotEffect;
    [SerializeField] private float hitEffectLifetime = .8f;

    [SerializeField] private GameObject bulletTrailPrefab;

    [SerializeField] private LayerMask hittableLayers;

    [SerializeField] private DamageLogSettings playerHitMarkerSettings;
    [SerializeField] private DamageLogManager damageLogManager;


    // Rpc -> remote procedure call

    //[ServerRpc] // called by a client to execute on the server; the caller MUST be the local player
    [Rpc(SendTo.Server)]
    public void RequestAttackServerRpc(Vector3 shootingPos, Vector3 shootingDir)
    {
        ExecuteAttackClientRpc(shootingPos, shootingDir);
    }

    [Rpc(SendTo.ClientsAndHost)] // called by the server to execute on all clients
    private void ExecuteAttackClientRpc(Vector3 shootingPos, Vector3 shootingDir)
    {
        damageLogManager.UpdatePlayerSettings(playerHitMarkerSettings);

        var bulletTrail = Instantiate(bulletTrailPrefab, shootingPos, Quaternion.identity).GetComponent<BulletTrail>();
        if (Physics.Raycast(shootingPos, shootingDir, out RaycastHit hit, float.PositiveInfinity, hittableLayers, QueryTriggerInteraction.Ignore))
        {
            bulletTrail.Set(shootingPos, hit.point);
            if (hit.collider.gameObject.TryGetComponent<IShootable>(out var shootableComponent))
            {
                shootableComponent.ReactShot(10, hit.point, shootingDir, NetworkObjectId);
            }

            //Destroy(Instantiate(landingShotEffect, hit.point - shootingDir * .1f, Quaternion.identity), hitEffectLifetime);

        }
        else
        {
            bulletTrail.Set(shootingPos, shootingPos + shootingDir * 100);
        }
    }

    public void SpawnDamageLog(TargetType targetType, ushort damage)
    {
        if (!IsOwner) {  return; }

        damageLogManager.SummonDamageLog(Vector3.zero, targetType, damage);
    }
}
