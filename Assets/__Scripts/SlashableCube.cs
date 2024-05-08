using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SlashableCube : MonoBehaviour, ISlashable
{
    public bool OnImmunityAfterHit { get; set; }

    public void ReactSlash(Vector3 _)
    {
        if (OnImmunityAfterHit) return;
        StartCoroutine(StartCooldown());
    }

    public IEnumerator StartCooldown()
    {
        OnImmunityAfterHit = true;

        yield return new WaitForSeconds(1f);

        OnImmunityAfterHit = false;
    }
}
