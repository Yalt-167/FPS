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
    private PlayerHealthNetworked playerHealth;
    public ulong OwnerHealthNetworkID => playerHealth.NetworkObjectId;

    public bool OnImmunityAfterHit { get; set; }
    public float ImmunityAfterHitDuration { get; set; } = .3f;

    private void Awake()
    {
        playerHealth = transform.parent.parent.GetComponent<PlayerHealthNetworked>();
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

    public void ReactShot(ushort damage, Vector3 _, Vector3 __, ulong attackerNetworkID, bool ___)
    {
        //if (!IsOwner) { return; }

        var damageAfterMultiplier = GetDamageAfterMultipliers(damage);

        DamageTargetServerRpc(damageAfterMultiplier, bodyPart, attackerNetworkID);
    }

    public void ReactExplosion(ushort damage, Vector3 _, ulong attackerNetworkID, bool __)
    {
        DamageTargetServerRpc(damage, bodyPart, attackerNetworkID);
    }

    public void ReactSlash(ushort damage, Vector3 _, ulong attackerNetworkID)
    {
        if (OnImmunityAfterHit) { return; }

        DamageTargetServerRpc(damage, bodyPart, attackerNetworkID);

        StartCoroutine(StartImmunityAfterSlashColdown());
    }

    [Rpc(SendTo.Server)]
    private void DamageTargetServerRpc(ushort damage, BodyParts bodyPart, ulong attackerNetworkID)
    {
        playerHealth.TakeDamageClientRpc(damage, bodyPart, false, attackerNetworkID);
    }

    public IEnumerator StartImmunityAfterSlashColdown()
    {
        OnImmunityAfterHit = true;

        yield return new WaitForSeconds(ImmunityAfterHitDuration);

        OnImmunityAfterHit = false;
    }

    /// <summary>
    /// This method aims to assert wether several hitboxes belong to a singular target<br> for instance a shotgun should nt log each and every pellet that land and rather log a total -> verifiy that each hit lands on the same target and add them all
    /// </summary>
    /// <returns></returns>
    public bool BelongToSamePlayer(List<IDamageable> components)
    {
        if (components.Count < 2) return true;

        var firstComponentNetworkID = components[0].OwnerHealthNetworkID;
        for (int i = 1; i < components.Count; i++)
        {
            if (components[i].OwnerHealthNetworkID != firstComponentNetworkID)
            {
                return false;
            }
        }

        return true;
    }

}
   

public enum BodyParts : byte
{
    HEAD,
    BODY,
    LEGS
}

// for the weapon do a base class that takes into account a scriptable object that dictate its stats and perhaps its model etc