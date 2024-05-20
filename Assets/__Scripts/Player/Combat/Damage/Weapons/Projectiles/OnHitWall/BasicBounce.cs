using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BasicBounce : ProjectileOnHitWallBehaviour
{
    public override void OnHitWall(Projectile relevantProjectile, Collider relevantWall)
    {
        var closestPoint = relevantWall.ClosestPoint(relevantProjectile.Position);
        if (Physics.Raycast(relevantProjectile.Position, relevantProjectile.Position - closestPoint, out var hit, 1f, Layers.Ground, QueryTriggerInteraction.Ignore))
        {
            relevantProjectile.SetDirection(Utility.ReflectVector(relevantProjectile.Direction, hit.normal));
        }
        else
        {
            print("Shouldn t have reached there");
            return;
        }
    }
}
