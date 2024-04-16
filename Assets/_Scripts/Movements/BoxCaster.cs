using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;


[DefaultExecutionOrder(-8)]
public class BoxCaster : MonoBehaviour
{
    [SerializeField] private bool debugBox;
    [field: SerializeField] public BoxCasterInstances Instance { get; private set; }
    [field: SerializeField] public Vector3 Size { get; private set; }


    private void Awake()
    {
        BoxCasterManager.Instance.RegistedBoxCaster(this);
    }

    public bool DiscardCast(LayerMask layers)
    {
        return Physics.CheckBox(
            transform.position,
            Size * .5f,
            transform.rotation,
            layers,
            QueryTriggerInteraction.Ignore
            );
    }

    public bool ReturnCast(LayerMask layers, out Collider[] colliders)
    {
        colliders = Physics.OverlapBox(
            transform.position,
            Size * .5f,
            transform.rotation,
            layers,
            QueryTriggerInteraction.Ignore
            );

        return colliders.Length > 0;
    }

    private void OnDrawGizmosSelected()
    {
        if (!debugBox) { return; }

        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(
            transform.position,
            Size
            );
    }
}