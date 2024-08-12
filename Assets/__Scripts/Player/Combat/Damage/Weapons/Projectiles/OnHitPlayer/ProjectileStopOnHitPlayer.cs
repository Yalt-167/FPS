using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Projectiles
{
    [Serializable]
    public sealed class ProjectileStopOnHitPlayer : ProjectileOnHitPlayerBehaviour
    {
        public ProjectileStopOnHitPlayer(IProjectileBehaviourOnHitPlayerParam _) { }

        public override void OnHitPlayer(Projectile relevantProjectile, IShootable _)
        {
            relevantProjectile.Deactivate();
        }
    }
}
