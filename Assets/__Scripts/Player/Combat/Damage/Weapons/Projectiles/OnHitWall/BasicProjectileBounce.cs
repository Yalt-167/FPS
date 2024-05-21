using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BasicProjectileBounce : ProjectileOnHitWallBehaviour
{
    [SerializeField] protected int maxBounces;
    protected int bounces;

    public override void OnHitWall(Projectile relevantProjectile, Collider relevantWall)
    {
        if (bounces == maxBounces) { return; }

        var closestPoint = relevantWall.ClosestPoint(relevantProjectile.Position);
        //if (Physics.Raycast(relevantProjectile.Position - (relevantProjectile.Position - closestPoint), relevantProjectile.Position - closestPoint, out var hit, 3f, Layers.Ground, QueryTriggerInteraction.Ignore))
        if (Physics.Raycast(relevantProjectile.Position, closestPoint - relevantProjectile.Position , out var hit, 3f, Layers.Ground, QueryTriggerInteraction.Ignore))
        {
            print(hit.normal);
            relevantProjectile.SetDirection(Utility.ReflectVector(relevantProjectile.Direction, hit.normal));
            bounces++;
        }
        else
        {
            print("Shouldn t have reached there");
            return;
        }
    }
}