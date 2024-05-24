using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public abstract class ProjectileOnHitPlayerBehaviour
{
    public abstract void Init(IProjectileBehaviourOnHitPlayerParam param_);
    public abstract void OnHitPlayer(Projectile relevantProjectile, IShootable relevantPlayer);
}
