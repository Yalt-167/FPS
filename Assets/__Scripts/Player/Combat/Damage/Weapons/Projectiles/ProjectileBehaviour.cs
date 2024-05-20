using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class ProjectileBehaviour : MonoBehaviour
{
    public abstract void OnHitWall(IProjectileOnHitWallParam param);

    public abstract void OnHitPlayer(IProjectileOnHitPlayerParam param);
}
