using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

using UnityEngine; 

namespace WeaponHandling
{
    public sealed class WeaponRecoil : MonoBehaviour
    {
        private Vector3 currentRecoilHandlerRotation;
        private Vector3 targetRecoilHandlerRotation;
        [SerializeField] private float recoilMovementSnappiness;


        [SerializeField] private Transform recoilHandlerTransform;
        private bool IsAiming => throw new NotImplementedException(); // link the 

        private bool isAiming;
        private float RecoilRegulationSpeed => isAiming ? currentWeaponStats.AimingRecoilStats.RecoilRegulationSpeed : currentWeaponStats.HipfireRecoilStats.RecoilRegulationSpeed;

        private WeaponStats currentWeaponStats;
        private RecoilStats AimingRecoilStats;
        private RecoilStats HipfireRecoilStats;

        private void ApplyRecoil()
        {
            if (isAiming)
            {
                targetRecoilHandlerRotation += new Vector3(
                    -currentWeaponStats.AimingRecoilStats.RecoilForceX,
                    UnityEngine.Random.Range(-currentWeaponStats.AimingRecoilStats.RecoilForceY, currentWeaponStats.AimingRecoilStats.RecoilForceY),
                    UnityEngine.Random.Range(-currentWeaponStats.AimingRecoilStats.RecoilForceZ, currentWeaponStats.AimingRecoilStats.RecoilForceZ)
                );
            }
            else
            {
                targetRecoilHandlerRotation += new Vector3(
                    -currentWeaponStats.HipfireRecoilStats.RecoilForceX,
                    UnityEngine.Random.Range(-currentWeaponStats.HipfireRecoilStats.RecoilForceY, currentWeaponStats.HipfireRecoilStats.RecoilForceY),
                    UnityEngine.Random.Range(-currentWeaponStats.HipfireRecoilStats.RecoilForceZ, currentWeaponStats.HipfireRecoilStats.RecoilForceZ)
                );
            }
        }
        private void ApplyRecoil(float chargeRatio)
        {
            if (isAiming)
            {
                targetRecoilHandlerRotation += new Vector3(
                    -currentWeaponStats.AimingRecoilStats.RecoilForceX * chargeRatio,
                    UnityEngine.Random.Range(-currentWeaponStats.AimingRecoilStats.RecoilForceY * chargeRatio, currentWeaponStats.AimingRecoilStats.RecoilForceY * chargeRatio),
                    UnityEngine.Random.Range(-currentWeaponStats.AimingRecoilStats.RecoilForceZ * chargeRatio, currentWeaponStats.AimingRecoilStats.RecoilForceZ * chargeRatio)
                );
            }
            else
            {
                targetRecoilHandlerRotation += new Vector3(
                    -currentWeaponStats.HipfireRecoilStats.RecoilForceX * chargeRatio,
                    UnityEngine.Random.Range(-currentWeaponStats.HipfireRecoilStats.RecoilForceY * chargeRatio, currentWeaponStats.HipfireRecoilStats.RecoilForceY * chargeRatio),
                    UnityEngine.Random.Range(-currentWeaponStats.HipfireRecoilStats.RecoilForceZ * chargeRatio, currentWeaponStats.HipfireRecoilStats.RecoilForceZ * chargeRatio)
                );
            }
        }

        private void HandleRecoil()
        {
            targetRecoilHandlerRotation = Vector3.Lerp(targetRecoilHandlerRotation, Vector3.zero, RecoilRegulationSpeed * Time.deltaTime);
            currentRecoilHandlerRotation = Vector3.Slerp(currentRecoilHandlerRotation, targetRecoilHandlerRotation, recoilMovementSnappiness * Time.deltaTime);
            recoilHandlerTransform.localRotation = Quaternion.Euler(currentRecoilHandlerRotation);
            MyDebug.DebugOSD.Display(currentRecoilHandlerRotation);
        }
    }
}