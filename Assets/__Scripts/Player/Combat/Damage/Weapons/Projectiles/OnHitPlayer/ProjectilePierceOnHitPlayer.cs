using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Projectiles
{
    [Serializable]
    public class ProjectilePierceOnHitPlayer : ProjectileOnHitPlayerBehaviour
    {
        protected ushort maxPlayerPierceAmount;
        protected ushort currentPiercedPlayerCount;

        public ProjectilePierceOnHitPlayer(IProjectileBehaviourOnHitPlayerParam param_)
        {
            var param = (ProjectilePlayerPierceParams)param_;

            maxPlayerPierceAmount = param.MaxPlayersToPierce;
        }

        public override void OnHitPlayer(Projectile relevantProjectile, IShootable relevantPlayer)
        {
            if (currentPiercedPlayerCount == maxPlayerPierceAmount)
            {
                relevantProjectile.Deactivate();
                return;
            }

            currentPiercedPlayerCount++;
        }
    }
}