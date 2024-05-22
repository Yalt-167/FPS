using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BasicProjectileStopOnHitWall : ProjectileOnHitWallBehaviour
{
    public override void OnHitWall(Projectile relevantProjectile, Collider _)
    {
        relevantProjectile.Deactivate();
    }
}
