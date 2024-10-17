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
        private float gunTravelDistanceWhenADSing;
        [SerializeField] private float sqrDistanceLeniency = .01f;


        private new Camera camera;


        private float FOVDifference;
        private static readonly int baseFOV = 60;


        private static readonly float fixedUpdateCallFrequency = .02f;



        private bool isADSing;
        private AimAndScopeStats aimingStats;

        public void SetupData(AimAndScopeStats aimingStats_)
        {
            basePositionTransform = transform.parent.GetChild(1);
            ADSPositionTransform = transform.parent.GetChild(2);

            camera = transform.parent.parent.GetComponent<Camera>();

            aimingStats = aimingStats_;

            gunTravelDistanceWhenADSing = Vector3.Distance(ADSPositionTransform.position, basePositionTransform.position);
            FOVDifference = baseFOV - aimingStats.AimingFOV;
            ResetPosition();
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
            float stepPerFixedUpdate;
            Vector3 targetPosition;
            if (isADSing)
            {
                targetPosition = ADSPositionTransform.position;

                if (ClampPosition(targetPosition)) { return; }

                stepPerFixedUpdate = gunTravelDistanceWhenADSing / (aimingStats.TimeToADS / fixedUpdateCallFrequency);
            }
            else
            {
                targetPosition = basePositionTransform.position;

                if (ClampPosition(targetPosition)) { return; }

                stepPerFixedUpdate = gunTravelDistanceWhenADSing / (aimingStats.TimeToUnADS / fixedUpdateCallFrequency);
            }

            transform.position = Vector3.MoveTowards(transform.position, targetPosition, stepPerFixedUpdate);
        }

        private bool ClampPosition(Vector3 targetPosition)
        {
            if ((transform.position - targetPosition).sqrMagnitude <= sqrDistanceLeniency)
            {
                transform.position = targetPosition;
                return true;
            }

            return false;
        }

        private void HandleFOVLerp()
        {
            float stepPerFixedUpdate;
            float targetFOV;
            if (isADSing)
            {
                targetFOV = aimingStats.AimingFOV;

                if (camera.fieldOfView <= targetFOV)
                {
                    camera.fieldOfView = targetFOV;
                    return;
                }

                stepPerFixedUpdate = FOVDifference / (aimingStats.TimeToADS / fixedUpdateCallFrequency);
            }
            else
            {
                targetFOV = baseFOV;

                if (camera.fieldOfView >= targetFOV)
                {
                    camera.fieldOfView = targetFOV;
                    return;
                }

                stepPerFixedUpdate = FOVDifference / (aimingStats.TimeToUnADS / fixedUpdateCallFrequency);
            }

            camera.fieldOfView = Mathf.MoveTowards(camera.fieldOfView, targetFOV, stepPerFixedUpdate);
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.green;
            if (basePositionTransform != null) { Gizmos.DrawWireSphere(basePositionTransform.position, .1f); }

            Gizmos.color = Color.yellow;
            if (ADSPositionTransform != null) { Gizmos.DrawWireSphere(ADSPositionTransform.position, .1f); }
        }
    }
}