using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class ProjectileOnHitWallBehaviour : MonoBehaviour
{
    public virtual void OnHitWall(Projectile relevantProjectile, Collider relevantWall) { }
}
