using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class PlayerCombatNetworked : NetworkBehaviour
{

    [SerializeField] private GameObject landingShotEffect;
    [SerializeField] private float hitEffectLifetime = .8f;

    [SerializeField] private GameObject bulletTrailPrefab;

    [SerializeField] private LayerMask hittableLayers;

    [SerializeField] private DamageLogSettings playerHitMarkerSettings;
    [SerializeField] private DamageLogManager damageLogManager;




    // Rpc -> remote procedure call

    [ServerRpc] // called by a client to execute on the server; the caller MUST be the local player
    public void RequestAttackServerRpc(Vector3 shootingPos, Vector3 shootingDir)
    {
        ExecuteAttackClientRpc(shootingPos, shootingDir);
    }

    [ClientRpc] // called by the server to execute on all clients
    private void ExecuteAttackClientRpc(Vector3 shootingPos, Vector3 shootingDir)
    {
        var bulletTrail = Instantiate(bulletTrailPrefab, shootingPos, Quaternion.identity).GetComponent<BulletTrail>();
        if (Physics.Raycast(shootingPos, shootingDir, out RaycastHit hit, float.PositiveInfinity, hittableLayers, QueryTriggerInteraction.Ignore))
        {
            bulletTrail.Set(transform.position, hit.point);
            if (hit.collider.gameObject.TryGetComponent<IShootable>(out var shootableComponent))
            {
                shootableComponent.ReactShot(hit.point, shootingDir);
            }

            damageLogManager.UpdatePlayerSettings(playerHitMarkerSettings);
            damageLogManager.SummonDamageLog(hit.point, TargetType.HEAD_SHIELDED, 75);
            //Destroy(Instantiate(landingShotEffect, hit.point - shootingDir * .1f, Quaternion.identity), hitEffectLifetime);

        }
        else
        {
            bulletTrail.Set(transform.position, shootingPos + shootingDir * 100);
        }
    }
}
