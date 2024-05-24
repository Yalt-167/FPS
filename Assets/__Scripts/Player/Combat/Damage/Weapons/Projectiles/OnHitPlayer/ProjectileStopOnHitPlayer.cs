using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class ProjectileStopOnHitPlayer : ProjectileOnHitPlayerBehaviour
{
    public override void Init(IProjectileBehaviourOnHitPlayerParam _) { }

    public override void OnHitPlayer(Projectile relevantProjectile, IShootable _)
    {
        relevantProjectile.Deactivate();
    }
}
