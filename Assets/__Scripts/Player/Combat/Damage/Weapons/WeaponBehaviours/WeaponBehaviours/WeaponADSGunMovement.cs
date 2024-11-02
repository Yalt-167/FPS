using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;

namespace WeaponHandling
{
    /// <summary>
    /// Add this script on the root/socket/handle of the weapon for it to handle the ADS logic
    /// </summary>
    public sealed class WeaponADSGunMovement : MonoBehaviour // moves the transfom which is already propagated + may do some clientside camera adjustment so no need to replicate
    {
        private Transform basePositionTransform;
        private Transform ADSPositionTransform;
        [SerializeField] private bool doDebugPosition;
        private float gunTravelDistanceWhenADSing;
        [SerializeField] private float sqrDistanceLeniency = .01f;

        private static readonly float fixedUpdateCallFrequency = .02f;

        private WeaponHandler weaponHandler;
        private AimAndScopeStats AimingStats => weaponHandler.CurrentWeapon.AimingAndScopeStats;
        private bool IsADSing => weaponHandler.IsAiming;

        public void SetupData(WeaponHandler weaponHandler_)
        {
            basePositionTransform = transform.parent.GetChild(1).GetChild(0);
            ADSPositionTransform = transform.parent.GetChild(1).GetChild(1);

            weaponHandler = weaponHandler_;

            gunTravelDistanceWhenADSing = Vector3.Distance(ADSPositionTransform.position, basePositionTransform.position);
            ResetPosition();
        }

        private void FixedUpdate()
        {
            HandleWeaponLerp();
        }


        private void ResetPosition()
        {
            transform.position = basePositionTransform.position;
        }

        private void HandleWeaponLerp()
        {
            float stepPerFixedUpdate;
            Vector3 targetPosition;
            if (IsADSing)
            {
                targetPosition = ADSPositionTransform.position;

                if (ClampPosition(targetPosition)) { return; }

                stepPerFixedUpdate = gunTravelDistanceWhenADSing / (AimingStats.TimeToADS / fixedUpdateCallFrequency);
            }
            else
            {
                targetPosition = basePositionTransform.position;

                if (ClampPosition(targetPosition)) { return; }

                stepPerFixedUpdate = gunTravelDistanceWhenADSing / (AimingStats.TimeToUnADS / fixedUpdateCallFrequency);
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

        private void OnDrawGizmosSelected()
        {
            if (!doDebugPosition) { return; }

            Gizmos.color = Color.green;
            if (basePositionTransform != null) { Gizmos.DrawWireSphere(basePositionTransform.position, .1f); }

            Gizmos.color = Color.yellow;
            if (ADSPositionTransform != null) { Gizmos.DrawWireSphere(ADSPositionTransform.position, .1f); }
        }
    }
}