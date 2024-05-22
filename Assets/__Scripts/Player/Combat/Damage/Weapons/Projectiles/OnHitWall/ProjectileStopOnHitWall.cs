using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class ProjectileStopOnHitWall : ProjectileOnHitWallBehaviour
{
    public override void OnHitWall(Projectile relevantProjectile, Collider _)
    {
        relevantProjectile.Deactivate();
    }
}
