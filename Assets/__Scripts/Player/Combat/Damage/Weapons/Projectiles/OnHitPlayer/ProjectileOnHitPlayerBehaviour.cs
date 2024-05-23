using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public abstract class ProjectileOnHitPlayerBehaviour : MonoBehaviour
{
    public abstract void OnHitPlayer(Projectile relevantProjectile, IShootable relevantPlayer);
}
