using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class ProjectileOnHitWallBehaviour : MonoBehaviour
{
    public abstract void OnHitWall(Projectile relevantProjectile, Collider relevantWall);
}
