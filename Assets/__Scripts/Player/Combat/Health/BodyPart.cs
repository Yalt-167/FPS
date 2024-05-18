using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using UnityEngine.UI;
using System.Diagnostics;
using System;

public class BodyPart : NetworkBehaviour, IShootable, IExplodable, ISlashable
{
    [SerializeField] private BodyParts bodyPart;
    private PlayerHealth playerHealth;

    public bool OnImmunityAfterHit { get; set; }
    public float ImmunityAfterHitDuration { get; set; } = .3f;

    private void Awake()
    {
        playerHealth = transform.parent.parent.GetComponent<PlayerHealth>();
    }

    public void ReactShot(ushort damage, Vector3 _, Vector3 __, ulong attackerNetworkID, bool ___)
    {
        //if (!IsOwner) { return; }

        var damageAfterMultiplier = GetDamageAfterMultipliers(damage);

        DamageTargetServerRpc(damageAfterMultiplier, bodyPart, attackerNetworkID);
    }

    [Rpc(SendTo.Server)]
    private void DamageTargetServerRpc(ushort damage, BodyParts bodyPart, ulong attackerNetworkID)
    {
        playerHealth.TakeDamageClientRpc(damage, bodyPart, false, attackerNetworkID);
    }

    private ushort GetDamageAfterMultipliers(ushort rawDamage)
    {
        return bodyPart switch
        {
            BodyParts.HEAD => (ushort)(rawDamage * 2),
            BodyParts.BODY => rawDamage,
            BodyParts.LEGS => (ushort)(.5f * rawDamage),
            _ => rawDamage,
        };
    }

    public void ReactExplosion(ushort damage, Vector3 _, ulong attackerNetworkID, bool __)
    {
        DamageTargetServerRpc(damage, bodyPart, attackerNetworkID);
    }


    public IEnumerator StartCooldown()
    {
        OnImmunityAfterHit = true;

        yield return new WaitForSeconds(ImmunityAfterHitDuration);

        OnImmunityAfterHit = false;
    }

    public void ReactSlash(ushort damage, Vector3 _, ulong attackerNetworkID)
    {
        if (OnImmunityAfterHit) { return; }

        DamageTargetServerRpc(damage, bodyPart, attackerNetworkID);

        StartCoroutine(StartCooldown());
    }
}

public enum BodyParts : byte
{
    HEAD,
    BODY,
    LEGS
}

// for the weapon do a base class that takes into account a scriptable object that dictate its stats and perhaps its model etc