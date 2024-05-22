using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BasicProjectilePierceOnHitPlayer : ProjectileOnHitPlayerBehaviour
{
    [SerializeField] protected ushort maxPlayerPierceAmount;
    protected ushort currentPiercedPlayerCount;

    public override void OnHitPlayer(Projectile relevantprojectile, IShootable relevantPlayer)
    {
        if (currentPiercedPlayerCount == maxPlayerPierceAmount)
        {
            relevantprojectile.Deactivate();
            return;
        }

        currentPiercedPlayerCount++;
    }
}
