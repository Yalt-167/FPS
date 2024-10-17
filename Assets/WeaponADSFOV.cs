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

        private float FOVDifference;
        private static readonly int baseFOV = 60;

        private static readonly float fixedUpdateCallFrequency = .02f;

        private bool isADSing;
        private AimAndScopeStats aimingStats;

        public void SetupData(AimAndScopeStats aimingStats_)
        {
            camera = GetComponent<Camera>();

            aimingStats = aimingStats_;

            FOVDifference = baseFOV - aimingStats.AimingFOV;
        }

        public void ToggleADS(bool towardOn)
        {
            isADSing = towardOn;
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

        private void FixedUpdate()
        {
            HandleFOVLerp();
        }
    }
}