using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class ProjectileOnHitWallBehaviour : MonoBehaviour
{
    //[field: SerializeField] protected IProjectileOnHitWallParam onHitWallParam { get; set; }
    public abstract void OnHitWall(Projectile relevantProjectile, Collider relevantWall);
}
