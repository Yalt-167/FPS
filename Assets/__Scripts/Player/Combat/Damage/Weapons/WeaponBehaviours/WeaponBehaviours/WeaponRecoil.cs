using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

using UnityEngine; 

namespace WeaponHandling
{
    public sealed class WeaponRecoil : WeaponBehaviour // moves the transfrom which is already propagated so doesn t need to be replicated
    {
        public override void Setup(Weapon weapon)
        {
            throw new NotImplementedException();
        }

        private Vector3 currentRecoilHandlerRotation;
        private Vector3 targetRecoilHandlerRotation;
        [SerializeField] private float recoilMovementSnappiness;


        [SerializeField] private Transform recoilHandlerTransform;
        private bool IsAiming => throw new NotImplementedException(); // link what needs to be

        private bool isAiming;
        private float RecoilRegulationSpeed => isAiming ? AimingRecoilStats.RecoilRegulationSpeed : HipfireRecoilStats.RecoilRegulationSpeed;

        private WeaponRecoilStats AimingRecoilStats;
        private WeaponRecoilStats HipfireRecoilStats;

        private void ApplyRecoil(float chargeRatio = 1)
        {
            if (isAiming)
            {
                targetRecoilHandlerRotation += new Vector3(
                    -AimingRecoilStats.RecoilForceX * chargeRatio,
                    UnityEngine.Random.Range(-AimingRecoilStats.RecoilForceY * chargeRatio, AimingRecoilStats.RecoilForceY * chargeRatio),
                    UnityEngine.Random.Range(-AimingRecoilStats.RecoilForceZ * chargeRatio, AimingRecoilStats.RecoilForceZ * chargeRatio)
                );
            }
            else
            {
                targetRecoilHandlerRotation += new Vector3(
                    -HipfireRecoilStats.RecoilForceX * chargeRatio,
                    UnityEngine.Random.Range(-HipfireRecoilStats.RecoilForceY * chargeRatio, HipfireRecoilStats.RecoilForceY * chargeRatio),
                    UnityEngine.Random.Range(-HipfireRecoilStats.RecoilForceZ * chargeRatio, HipfireRecoilStats.RecoilForceZ * chargeRatio)
                );
            }
        }

        private void HandleRecoil()
        {
            targetRecoilHandlerRotation = Vector3.Lerp(targetRecoilHandlerRotation, Vector3.zero, RecoilRegulationSpeed * Time.deltaTime);
            currentRecoilHandlerRotation = Vector3.Slerp(currentRecoilHandlerRotation, targetRecoilHandlerRotation, recoilMovementSnappiness * Time.deltaTime);
            recoilHandlerTransform.localRotation = Quaternion.Euler(currentRecoilHandlerRotation);
        }
    }
}