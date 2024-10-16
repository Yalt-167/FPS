using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;

namespace WeaponHandling
{
    /// <summary>
    /// Add this script on the root/socket/handle of the weapon for it to handle the ADS logic
    /// </summary>
    public sealed class WeaponADS : MonoBehaviour // moves the transfom which is already propagated + may do some clientside camera adjustment so no need to replicate
    {
        private Transform basePositionTransform;
        private Transform ADSPositionTransform;
        [SerializeField] private bool doDebugPosition;
        private new Camera camera;
        private readonly int baseFOV = 60;
        private float distancePerFixedUpdateCallForOneSecondAction;

        private readonly int fixedUpdateCallsPerSecond = 50;
        private Vector3 stepPerFixedUpdate;
        private Transform targetPosition;

        private AimAndScopeStats aimingStats;
#if false
        public struct AimAndScopeStats
		{
            public float AimingFOV; //perhaps just a cameraMove instead
            [Range(1, 5)][Tooltip("Set to 1 for no Scope")] public float ScopeMagnification;
            public float TimeToADS;
            public float TimeToUnADS;
        } 
#endif
        [SerializeField] private float sqrLerpLeniency = .01f;

        private bool isADSing;

        public void SetupData(AimAndScopeStats aimingStats_)
        {
            aimingStats = aimingStats_;
            distancePerFixedUpdateCallForOneSecondAction = Vector3.Distance(ADSPositionTransform.position, basePositionTransform.position) / fixedUpdateCallsPerSecond;
            ResetPosition();
            basePositionTransform = transform.parent.GetChild(1);
            ADSPositionTransform = transform.parent.GetChild(2);
            camera = transform.parent.parent.GetComponent<Camera>();


        }

        public void ToggleADS(bool towardOn)
        {
            isADSing = towardOn;
        }

        private void FixedUpdate()
        {
            HandleWeaponLerp();
            HandleFOVLerp();
        }


        private void ResetPosition()
        {
            transform.position = basePositionTransform.position;
        }

        private void HandleWeaponLerp()
        {
            var startingPoint = transform.position;


            var (
                targetPosition,
                transitionSpeed
                ) = isADSing ?
                (ADSPositionTransform, aimingStats.TimeToADS) : (basePositionTransform, aimingStats.TimeToUnADS);

            transform.position = Vector3.Lerp(startingPoint, targetPosition.position, transitionSpeed);

            //(targetPosition, stepPerFixedUpdate) = isADSing ?
            //    (ADSPosition, (ADSPosition.position - basePosition.position) / fixedUpdateCallsPerSecond / aimingStats.TimeToADS):
            //    (basePosition, (basePosition.position - ADSPosition.position) / fixedUpdateCallsPerSecond / aimingStats.TimeToUnADS);

            //(targetPosition, stepPerFixedUpdate) = isADSing ?
            //    (
            //        ADSPosition,
            //        (ADSPosition.position - basePosition.position).normalized / (distancePerFixedUpdateCallForOneSecondAction / aimingStats.TimeToADS)
            //    )
            //    :
            //    (
            //        basePosition,
            //        (basePosition.position - ADSPosition.position).normalized / (distancePerFixedUpdateCallForOneSecondAction / aimingStats.TimeToUnADS)
            //    );


            //if ((transform.position - targetPosition.position).sqrMagnitude < sqrLerpLeniency)
            //{
            //    transform.position = targetPosition.position;
            //}
            //else
            //{

            //    transform.position += stepPerFixedUpdate;
            //}
        }

        private void HandleFOVLerp()
        {
            //var (targetFOV, transitionSpeed) = isADSing ? (aimingStats.AimingFOV, aimingStats.TimeToADS) : (baseFOV, aimingStats.TimeToUnADS);

            //camera.fieldOfView = Mathf.Lerp(camera.fieldOfView, targetFOV, transitionSpeed);

            //if (camera.fieldOfView - targetFOV < sqrLerpLeniency)
            //{
            //    camera.fieldOfView = targetFOV;
            //}

        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.green;
            if (basePositionTransform != null) Gizmos.DrawWireSphere(basePositionTransform.position, .1f);

            Gizmos.color = Color.yellow;
            if (ADSPositionTransform != null) Gizmos.DrawWireSphere(ADSPositionTransform.position, .1f);
        }
    }
}


//public sealed class ADSAnchor: MonoBehaviour
//{
//    [SerializeField] private Transform[] anchors;

//    private int currentAnchorIndex = 0;
//    public Transform Anchor => anchors[currentAnchorIndex++ % anchors.Length];

//}