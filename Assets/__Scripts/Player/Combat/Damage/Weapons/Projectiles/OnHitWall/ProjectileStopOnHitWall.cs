using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Projectiles
{
    [Serializable]
    public sealed class ProjectileStopOnHitWall : ProjectileOnHitWallBehaviour
    {
        public ProjectileStopOnHitWall(IProjectileBehaviourOnHitWallParam _) { }

        public override void OnHitWall(Projectile relevantProjectile, Collider _)
        {
            relevantProjectile.Deactivate();
        }
    } 
}