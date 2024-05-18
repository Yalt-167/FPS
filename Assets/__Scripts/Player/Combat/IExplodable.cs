using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IExplodable
{
    public void ReactExplosion(ushort damage, Vector3 hitPoint, ulong attackerNetworkID, bool canBreakThings);
}
