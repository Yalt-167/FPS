using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public sealed class BodyPart : NetworkBehaviour, IShootable, IExplodable, ISlashable
{
    [SerializeField] private BodyParts bodyPart;
    private PlayerHealthNetworked playerHealth;
    public ulong OwnerHealthNetworkID => playerHealth.NetworkObjectId;
    public ushort OwnerTeamID => playerHealth.TeamID;

    public bool OnImmunityAfterHit { get; set; }
    public float ImmunityAfterHitDuration { get; set; } = .3f;

    private void Awake()
    {
        playerHealth = transform.parent.parent.GetComponent<PlayerHealthNetworked>();
    }

    private ushort GetEffectiveDamage(DamageDealt rawDamage)
    {
        return bodyPart switch
        {
            BodyParts.HEAD => rawDamage.HighDamage,
            BodyParts.BODY => rawDamage.BaseDamage,
            BodyParts.LEGS => rawDamage.LowDamage,
            _ => rawDamage.BaseDamage,
        };
    }

    public void ReactShot(DamageDealt damage, Vector3 _, Vector3 __, ulong attackerNetworkID, ushort attackerTeamID, bool ___)
    {
        //if (!IsOwner) { return; }

        if (attackerTeamID == OwnerTeamID)
        {
            Debug.Log("Shot teammate");
            return;
        }

        DamageTargetServerRpc(GetEffectiveDamage(damage), bodyPart, attackerNetworkID);
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
    /// This method aims to assert wether several hitboxes belong to a singular target<br> for instance a shotgun should nt log each and every pellet that lands and rather log a total -> verifiy that each hit lands on the same target and add them all
    /// </summary>
    /// <returns></returns>
    public bool BelongToSamePlayer(List<IDamageable> components)
    {
        if (components.Count < 2) return true;

        var firstComponentNetworkID = components[0].OwnerHealthNetworkID;
        foreach (var component in components)
        {
            if (component.OwnerHealthNetworkID != firstComponentNetworkID)
            {
                return false;
            }
        }

        return true;
    }

    // method that returns the ID of the player ot belongs too

}
  

public enum BodyParts : byte
{
    HEAD,
    BODY,
    LEGS
}

// for the weapon do a base class that takes into account a scriptable object that dictate its stats and perhaps its model etc