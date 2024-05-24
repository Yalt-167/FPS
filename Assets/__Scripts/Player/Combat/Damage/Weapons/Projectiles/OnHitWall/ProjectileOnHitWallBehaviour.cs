using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public abstract class ProjectileOnHitWallBehaviour
{
    public abstract void Init(IProjectileBehaviourOnHitWallParam param_);
    public abstract void OnHitWall(Projectile relevantProjectile, Collider relevantWall);
}
