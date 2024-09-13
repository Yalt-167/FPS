using System.Collections;
using System.Collections.Generic;

using UnityEngine;

public sealed class HandleGunAngle : MonoBehaviour
{
    [SerializeField] private LayerMask shootableLayers;
    private Transform cameraTransform;
    private Transform barrelEnd;

    private void Awake()
    {
        cameraTransform = transform.parent;
        barrelEnd = transform.GetChild(0).GetChild(0);
    }

    private void Update()
    {
        transform.LookAt(GetPointOnCrosshair());
        barrelEnd.LookAt(GetPointOnCrosshair());
    }

    private Vector3 GetPointOnCrosshair()
    {
        return GetPointFromPointAndAngle(cameraTransform.position, cameraTransform.forward);
    }


    private Vector3 GetPointFromPointAndAngle(Vector3 point, Vector3 angle)
    {
        if (Physics.Raycast(point, angle, out var hit, float.PositiveInfinity, shootableLayers, QueryTriggerInteraction.Ignore))
        {
            return hit.point;
        }
        else
        {
            return point + angle * 10f;
        }
    }
}
