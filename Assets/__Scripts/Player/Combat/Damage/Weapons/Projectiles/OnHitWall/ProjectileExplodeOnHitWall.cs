using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class ProjectileExplodeOnHitWall : ProjectileOnHitWallBehaviour
{
    protected float explosionRadius;
    protected ushort explosionDamage;

    public ProjectileExplodeOnHitWall(IProjectileBehaviourOnHitWallParam param_)
    {
        var param = (ProjectileWallExplodeParams)param_;

        explosionRadius = param.ExplosionRadius;
        explosionDamage = param.ExplosionDamage;
    }

    public override void OnHitWall(Projectile relevantProjectile, Collider relevantWall)
    {
        var hits = Physics.OverlapSphere(relevantProjectile.Position, explosionRadius, Layers.PlayerHitBoxes, QueryTriggerInteraction.Ignore);
        for (int i = 0; i < hits.Length; i++)
        {
            if (hits[i].TryGetComponent<IExplodable>(out var explodableComponent))
            {
                explodableComponent.ReactExplosion(explosionDamage, relevantProjectile.Position, relevantProjectile.Owner, relevantProjectile.CanBreakThings);
            }
        }

        relevantProjectile.Deactivate();
    }
}
