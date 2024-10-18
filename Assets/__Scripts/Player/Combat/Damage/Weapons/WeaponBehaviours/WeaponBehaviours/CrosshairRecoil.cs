

using System;
using System.Collections;
using System.Collections.Generic;


using UnityEngine;


using Unity.Netcode;


namespace WeaponHandling
{
    public sealed class CrosshairRecoil : WeaponReplicatedBehaviour
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
        public void ApplyRecoilClientRpc(float chargeRatio = 1) // woudl result in different recoil on each client
        {
            var relevantRecoilStats = isAiming ? AimingRecoilStats : HipfireRecoilStats;

            float y = relevantRecoilStats.RecoilForceY * chargeRatio;
            float z = relevantRecoilStats.RecoilForceZ * chargeRatio;

            targetRecoilHandlerRotation.Value += new Vector3(
                 -HipfireRecoilStats.RecoilForceX * chargeRatio,
                 UnityEngine.Random.Range(-y, y),
                 UnityEngine.Random.Range(-z, z)
            );
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


        private void FindRecoilHandlerTransform()
        {

        }
    }                       
}