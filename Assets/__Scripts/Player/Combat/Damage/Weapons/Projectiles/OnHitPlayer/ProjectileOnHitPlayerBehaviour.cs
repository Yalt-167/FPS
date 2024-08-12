using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Projectiles
{
	[Serializable]
	public abstract class ProjectileOnHitPlayerBehaviour
	{
		public abstract void OnHitPlayer(Projectile relevantProjectile, IShootable relevantPlayer);
	}
}