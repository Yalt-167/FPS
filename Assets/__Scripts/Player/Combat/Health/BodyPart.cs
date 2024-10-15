using System;
using System.Collections;
using System.Collections.Generic;

using Unity.Netcode;

using UnityEngine;

public sealed class BodyPart : NetworkBehaviour, IShootable, IExplodable, ISlashable
{
    [SerializeField] private BodyParts bodyPart;
    private PlayerHealthNetworked playerHealth;
    public ulong OwnerHealthNetworkID => playerHealth.NetworkObjectId;
    public ushort OwnerTeamNumber => playerHealth.TeamNumber;

    public bool OnImmunityAfterHit { get; set; }
    public float ImmunityAfterHitDuration { get; set; } = .3f;

    private void Awake()
    {
        playerHealth = transform.parent.parent.GetComponent<PlayerHealthNetworked>();
    }

    public void ReactShot(DamageDealt rawDamage, Vector3 _, Vector3 __, ulong attackerNetworkObjectID, ushort attackerTeamNumber, bool ___)
    {
        //if (!IsOwner) { return; }

        if (attackerTeamNumber == OwnerTeamNumber)
        {
            Debug.Log($"Shot teammate: Team{attackerTeamNumber}");
            return;
        }

        DamageTargetServerRpc(rawDamage[bodyPart], bodyPart, attackerNetworkObjectID);
    }

    public void ReactExplosion(ushort damage, Vector3 _, ulong attackerNetworkObjectID, bool __)
    {
        DamageTargetServerRpc(damage, bodyPart, attackerNetworkObjectID);
    }

    public void ReactSlash(ushort damage, Vector3 _, ulong attackerNetworkObjectID)
    {
        if (OnImmunityAfterHit) { return; }

        DamageTargetServerRpc(damage, bodyPart, attackerNetworkObjectID);

        StartCoroutine(StartImmunityAfterSlashColdown());
    }

    [Rpc(SendTo.Server)]
    private void DamageTargetServerRpc(ushort damage, BodyParts bodyPart, ulong attackerNetworkObjectID)
    {
        playerHealth.TakeDamageClientRpc(damage, bodyPart, false, attackerNetworkObjectID);
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