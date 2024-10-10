

using System;
using System.Collections;
using System.Collections.Generic;


using UnityEngine;


using Unity.Netcode;


namespace WeaponHandling
{
    public sealed class WeaponRecoil : WeaponReplicatedBehaviour
    {
        private NetworkVariable<Vector3> currentRecoilHandlerRotation = new NetworkVariable<Vector3>(readPerm: NetworkVariableReadPermission.Everyone, writePerm: NetworkVariableWritePermission.Server);
        private NetworkVariable<Vector3> targetRecoilHandlerRotation = new NetworkVariable<Vector3>(readPerm: NetworkVariableReadPermission.Everyone, writePerm: NetworkVariableWritePermission.Server);
        [SerializeField] private float recoilMovementSnappiness;


        [SerializeField] private Transform recoilHandlerTransform;
        private bool IsAiming => throw new NotImplementedException(); // link what needs to be and actually use this instead

        private bool isAiming;

        // as networkVar to prevent tampering with those
        private WeaponRecoilStats AimingRecoilStats; 
        private WeaponRecoilStats HipfireRecoilStats;

        [Rpc(SendTo.ClientsAndHost)]
        public void ApplyRecoilClientRpc(float chargeRatio = 1)
        {
            if (isAiming)
            {
                targetRecoilHandlerRotation.Value += new Vector3(
                    -AimingRecoilStats.RecoilForceX * chargeRatio,
                    UnityEngine.Random.Range(-AimingRecoilStats.RecoilForceY * chargeRatio, AimingRecoilStats.RecoilForceY * chargeRatio),
                    UnityEngine.Random.Range(-AimingRecoilStats.RecoilForceZ * chargeRatio, AimingRecoilStats.RecoilForceZ * chargeRatio)
                );
            }
            else
            {
                targetRecoilHandlerRotation.Value += new Vector3(
                    -HipfireRecoilStats.RecoilForceX * chargeRatio,
                    UnityEngine.Random.Range(-HipfireRecoilStats.RecoilForceY * chargeRatio, HipfireRecoilStats.RecoilForceY * chargeRatio),
                    UnityEngine.Random.Range(-HipfireRecoilStats.RecoilForceZ * chargeRatio, HipfireRecoilStats.RecoilForceZ * chargeRatio)
                );
            }
        }

        [Rpc(SendTo.ClientsAndHost)]
        private void HandleRecoilClientRpc()
        {
            targetRecoilHandlerRotation.Value = Vector3.Lerp(targetRecoilHandlerRotation.Value, Vector3.zero, (isAiming ? AimingRecoilStats.RecoilRegulationSpeed : HipfireRecoilStats.RecoilRegulationSpeed) * Time.deltaTime);
            currentRecoilHandlerRotation.Value = Vector3.Slerp(currentRecoilHandlerRotation.Value, targetRecoilHandlerRotation.Value, recoilMovementSnappiness * Time.deltaTime);
            recoilHandlerTransform.localRotation = Quaternion.Euler(currentRecoilHandlerRotation.Value);
        }

        public void SetData(WeaponRecoilStats AimingRecoilStats_, WeaponRecoilStats HipfireRecoilStats_)
        {
            AimingRecoilStats = AimingRecoilStats_;
            HipfireRecoilStats = HipfireRecoilStats_;
        }
    }                       
}