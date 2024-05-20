using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface ISlashable : IDamageable
{
    public void ReactSlash(ushort damage, Vector3 directionPlayerFaced, ulong attackerNetworkID);

    public IEnumerator StartImmunityAfterSlashColdown();

    public bool OnImmunityAfterHit { get; set; }
    public float ImmunityAfterHitDuration { get; set; }
}
