using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Projectiles
{
    [Serializable]
    public class ProjectilePierceOnHitWall : ProjectileOnHitWallBehaviour
    {
        protected ushort maxWallAmountToPierce;
        protected ushort wallPierced;

        public ProjectilePierceOnHitWall(IProjectileBehaviourOnHitWallParam param_)
        {
            var param = (ProjectileWallPierceParams)param_;

            maxWallAmountToPierce = param.MaxWallsToPierce;
        }

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
}