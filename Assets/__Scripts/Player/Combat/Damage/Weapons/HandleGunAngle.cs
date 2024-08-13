using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public sealed class HandleGunAngle : MonoBehaviour
{

    [SerializeField] private LayerMask shootableLayers;
    private Transform cameraTransform;

    private void Awake()
    {
        cameraTransform = transform.parent;
    }

    private void LateUpdate()
    {
        transform.LookAt(GetPointOnCrosshair());
    }

    private Vector3 GetPointOnCrosshair()
    {
        if(Physics.Raycast(cameraTransform.position, cameraTransform.forward, out var hit, float.PositiveInfinity, shootableLayers, QueryTriggerInteraction.Ignore))
        {
            return hit.point;
        }
        else
        {
            return cameraTransform.position + cameraTransform.forward * 1000f;
        }
    }

    private void OnDrawGizmos()
    {
        if (!Application.isPlaying) {  return; }

        Gizmos.color = Color.red;
        Gizmos.DrawLine(cameraTransform.position, cameraTransform.forward * 1000f);
    }
}
