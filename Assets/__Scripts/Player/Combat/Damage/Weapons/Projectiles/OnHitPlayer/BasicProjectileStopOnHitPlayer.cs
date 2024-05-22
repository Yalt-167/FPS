using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BasicProjectileStopOnHitPlayer : ProjectileOnHitPlayerBehaviour
{
    public override void OnHitPlayer(Projectile relevantProjectile, IShootable _)
    {
        relevantProjectile.Deactivate();
    }
}
