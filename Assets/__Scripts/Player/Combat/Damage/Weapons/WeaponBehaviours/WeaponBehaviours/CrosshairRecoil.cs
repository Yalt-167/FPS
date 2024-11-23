

using System;
using System.Collections;
using System.Collections.Generic;


using UnityEngine;


using Unity.Netcode;


namespace WeaponHandling
{
    public sealed class CrosshairRecoil : NetworkBehaviour
    {
        private NetworkVariable<Vector3> currentRecoilHandlerRotation = new NetworkVariable<Vector3>(readPerm: NetworkVariableReadPermission.Everyone, writePerm: NetworkVariableWritePermission.Server);
        private NetworkVariable<Vector3> targetRecoilHandlerRotation = new NetworkVariable<Vector3>(readPerm: NetworkVariableReadPermission.Everyone, writePerm: NetworkVariableWritePermission.Server);
        [SerializeField] private float recoilMovementSnappiness = 6f;


        private Transform recoilHandlerTransform;

        private WeaponHandler weaponHandler;
        private bool IsAiming => weaponHandler.IsAiming;
        private WeaponRecoilStats AimingRecoilStats => weaponHandler.CurrentWeaponSO.AimingRecoilStats;
        private WeaponRecoilStats HipfireRecoilStats => weaponHandler.CurrentWeaponSO.HipfireRecoilStats;
        

        private void Awake()
        {
            recoilHandlerTransform = transform.GetChild(0).GetChild(0);
            weaponHandler = GetComponent<WeaponHandler>();
        }

        public void SetupData(WeaponHandler weaponHandler_)
        {

        }

        [Rpc(SendTo.Server)]
        public void ApplyRecoilServerRpc(float chargeRatio = 1f)
        {
            var relevantRecoilStats = IsAiming ? AimingRecoilStats : HipfireRecoilStats;

            float y = relevantRecoilStats.RecoilForceY * chargeRatio;
            float z = relevantRecoilStats.RecoilForceZ * chargeRatio;

            targetRecoilHandlerRotation.Value += new Vector3(
                 -relevantRecoilStats.RecoilForceX * chargeRatio,
                 UnityEngine.Random.Range(-y, y),
                 UnityEngine.Random.Range(-z, z)
            );
        }

        public void ApplyRecoilFromServer(float chargeRatio = 1f)
        {
            var relevantRecoilStats = IsAiming ? AimingRecoilStats : HipfireRecoilStats;

            float y = relevantRecoilStats.RecoilForceY * chargeRatio;
            float z = relevantRecoilStats.RecoilForceZ * chargeRatio;

            targetRecoilHandlerRotation.Value += new Vector3(
                 -relevantRecoilStats.RecoilForceX * chargeRatio,
                 UnityEngine.Random.Range(-y, y),
                 UnityEngine.Random.Range(-z, z)
            );
        }

        [Rpc(SendTo.Server)]
        public void HandleRecoilServerRpc()
        {
            targetRecoilHandlerRotation.Value = Vector3.Lerp(targetRecoilHandlerRotation.Value, Vector3.zero, (IsAiming ? AimingRecoilStats.RecoilRegulationSpeed : HipfireRecoilStats.RecoilRegulationSpeed) * Time.deltaTime);
            currentRecoilHandlerRotation.Value = Vector3.Slerp(currentRecoilHandlerRotation.Value, targetRecoilHandlerRotation.Value, recoilMovementSnappiness * Time.deltaTime);
            UpdateRecoilClientRpc(Quaternion.Euler(currentRecoilHandlerRotation.Value));
        }

        [Rpc(SendTo.ClientsAndHost)]
        private void UpdateRecoilClientRpc(Quaternion rotation)
        {
            recoilHandlerTransform.localRotation = rotation;
        } 
    }                       
}