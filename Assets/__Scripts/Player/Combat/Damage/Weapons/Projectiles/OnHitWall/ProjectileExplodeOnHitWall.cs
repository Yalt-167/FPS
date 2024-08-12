using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public sealed class ProjectileExplodeOnHitWall : ProjectileOnHitWallBehaviour
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
        foreach (var hit in hits)
        {
            if (hit.TryGetComponent<IExplodable>(out var explodableComponent))
            {
                explodableComponent.ReactExplosion(explosionDamage, relevantProjectile.Position, relevantProjectile.Owner, relevantProjectile.CanBreakThings);
            }
        }

        relevantProjectile.Deactivate();
    }
}
