using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SlashableCube : MonoBehaviour, ISlashable
{

    public bool OnImmunityAfterHit { get; set; }
    public float ImmunityAfterHitDuration { get => throw new System.NotImplementedException(); set => throw new System.NotImplementedException(); }

    public ulong OwnerHealthNetworkID => throw new System.NotImplementedException();

    public void ReactSlash(Vector3 _)
    {
        if (OnImmunityAfterHit) return;
        StartCoroutine(StartImmunityAfterSlashColdown());
    }

    public void ReactSlash(ushort damage, Vector3 directionPlayerFaced, ulong attackerNetworkID)
    {
        throw new System.NotImplementedException();
    }

    public IEnumerator StartImmunityAfterSlashColdown()
    {
        OnImmunityAfterHit = true;

        yield return new WaitForSeconds(1f);

        OnImmunityAfterHit = false;
    }
}
