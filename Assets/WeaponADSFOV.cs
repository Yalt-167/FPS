using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;

namespace WeaponHandling
{
    /// <summary>
    /// Add this script on the GO holding the camera
    /// </summary>
    public sealed class WeaponADSFOV : MonoBehaviour
    {
        private new Camera camera;
        private bool isSetUp;
        private float FOVDifference;
        private static readonly int baseFOV = 60;

        private static readonly float fixedUpdateCallFrequency = .02f;

        private WeaponHandler weaponHandler;
        private bool IsADSing => weaponHandler.IsAiming;
        private AimAndScopeStats AimingStats => weaponHandler.CurrentWeaponSO.AimingAndScopeStats;

        public void SetupData(WeaponHandler weaponHandler_)
        {
            camera = GetComponent<Camera>();

            weaponHandler = weaponHandler_;

            FOVDifference = baseFOV - AimingStats.AimingFOV;

            isSetUp = true;
        }



        private void HandleFOVLerp()
        {
            if (!isSetUp) { return; }

            float stepPerFixedUpdate;
            float targetFOV;
            if (IsADSing)
            {
                targetFOV = AimingStats.AimingFOV;

                if (camera.fieldOfView <= targetFOV)
                {
                    camera.fieldOfView = targetFOV;
                    return;
                }

                stepPerFixedUpdate = FOVDifference / (AimingStats.TimeToADS / fixedUpdateCallFrequency);
            }
            else
            {
                targetFOV = baseFOV;

                if (camera.fieldOfView >= targetFOV)
                {
                    camera.fieldOfView = targetFOV;
                    return;
                }

                stepPerFixedUpdate = FOVDifference / (AimingStats.TimeToUnADS / fixedUpdateCallFrequency);
            }

            camera.fieldOfView = Mathf.MoveTowards(camera.fieldOfView, targetFOV, stepPerFixedUpdate);
        }

        private void FixedUpdate()
        {
            HandleFOVLerp();
        }
    }
}