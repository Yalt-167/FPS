using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class BodyPart : NetworkBehaviour, IShootable
{
    [SerializeField] private DamageMultipliers relevantDamageMultiplier;
    [SerializeField] private PlayerHealth playerHealth;

    
    public void ReactShot(Vector3 _, Vector3 __)
    {
        print(relevantDamageMultiplier);
        if (!IsOwner) { return; }

        DamageTargetServerRpc();
    }

    [ServerRpc]
    private void DamageTargetServerRpc()
    {
        playerHealth.TakeDamageClientRpc(75, false);
    }
}

public enum DamageMultipliers
{
    HEAD,
    BODY,
    LEGS
}

// for the weapon do a base class that takes into account a scriptable object that dictate its stats and perhaps its model etc