using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public sealed class ProjectileBounceOnHitWall : ProjectileOnHitWallBehaviour
{
    protected int maxBounces;
    protected int bounces;

    public ProjectileBounceOnHitWall(IProjectileBehaviourOnHitWallParam param_)
    {
        var param = (ProjectileWallBounceParams)param_;

        maxBounces = param.MaxBounces;
    }

    public override void OnHitWall(Projectile relevantProjectile, Collider relevantWall)
    {
        if (bounces == maxBounces) { return; }

        var closestPoint = relevantWall.ClosestPoint(relevantProjectile.Position);
        if (Physics.Raycast(relevantProjectile.Position, closestPoint - relevantProjectile.Position , out var hit, 3f, Layers.Ground, QueryTriggerInteraction.Ignore))
        {
            relevantProjectile.SetDirection(Utility.ReflectVector(relevantProjectile.Direction, hit.normal));
            bounces++;
        }
        else
        {
            //print("Shouldn t have reached there");
            return;
        }
    }
}