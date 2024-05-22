using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BasicProjectileExplodeOnHitPlayer : ProjectileOnHitPlayerBehaviour
{
    [SerializeField] protected float explosionRadius;
    [SerializeField] protected ushort explosionDamage;

    public override void OnHitPlayer(Projectile relevantProjectile, IShootable relevantPlayer)
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
