using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BasicProjectilePierceWall : ProjectileOnHitWallBehaviour
{
    [SerializeField] protected ushort maxWallAmountToPierce;
    protected ushort wallPierced;


    // if several coliders on a singular wall it would count all colliders so keep that in check
    public override void OnHitWall(Projectile relevantProjectile, Collider __)
    {
        wallPierced++;

        if (wallPierced == maxWallAmountToPierce)
        {
            relevantProjectile.Deactivate();
        }
    }
}