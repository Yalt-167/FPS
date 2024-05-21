using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class ProjectileOnHitPlayerBehaviour : MonoBehaviour
{
    public abstract void OnHitPlayer(Projectile relevantprojectile, IShootable relevantPlayer);
}
