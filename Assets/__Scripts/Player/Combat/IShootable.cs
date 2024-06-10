using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public interface IShootable : IDamageable
{
    public void ReactShot(DamageDealt damage, Vector3 shootingAngle, Vector3 hitPoint, ulong attackerNetworkID, ushort attackerTeamID, bool canBreakThings);
}