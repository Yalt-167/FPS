using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public abstract class ProjectileOnHitWallBehaviour : MonoBehaviour
{
    public abstract void OnHitWall(Projectile relevantProjectile, Collider relevantWall);
}
