using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;


[DefaultExecutionOrder(-8)]
public class BoxCaster : MonoBehaviour
{
    [SerializeField] private bool debugBox;
    [field: SerializeField] public Vector3 Size { get; private set; }

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

    public bool AddCast(LayerMask layers, ref List<Collider> colliders)
    {
        var tempColliders  = Physics.OverlapBox(
            transform.position,
            Size * .5f,
            transform.rotation,
            layers,
            QueryTriggerInteraction.Ignore
            );

        foreach (var collider in tempColliders)
        {
            colliders.Add(collider);
        }

        return tempColliders.Length > 0;
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