using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface ISlashable
{
    public void ReactSlash(Vector3 directionPlayerFaced);

    public IEnumerator StartCooldown();

    public bool OnImmunityAfterHit {  get; set; }
}
