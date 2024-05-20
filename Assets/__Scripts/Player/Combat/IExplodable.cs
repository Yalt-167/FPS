using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IExplodable : IDamageable
{
    public void ReactExplosion(ushort damage, Vector3 explosionPoint, ulong attackerNetworkID, bool canBreakThings);
}
