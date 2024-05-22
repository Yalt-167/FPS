using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ProjectileStopOnHitPlayer : ProjectileOnHitPlayerBehaviour
{
    public override void OnHitPlayer(Projectile relevantProjectile, IShootable _)
    {
        relevantProjectile.Deactivate();
    }
}
