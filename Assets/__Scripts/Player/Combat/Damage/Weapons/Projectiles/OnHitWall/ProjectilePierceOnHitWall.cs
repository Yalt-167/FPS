using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class ProjectilePierceOnHitWall : ProjectileOnHitWallBehaviour
{
    [SerializeField] protected ushort maxWallAmountToPierce;
    protected ushort wallPierced;

    // if several colliders on a singular wall it would count all colliders so keep that in check
    public override void OnHitWall(Projectile relevantProjectile, Collider __)
    {
        if (wallPierced == maxWallAmountToPierce)
        {
            relevantProjectile.Deactivate();
        }

        wallPierced++;
    }
}