using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class ProjectileOnHitPlayerBehaviour : MonoBehaviour
{
    //[field: SerializeField] protected IProjectileOnHitWallParam onHitPlayerParam { get; set; }
    public abstract void OnHitPlayer(Projectile relevantprojectile, IShootable relevantPlayer);
}
