using System;
using System.Collections;
using System.Collections.Generic;

using Unity.Netcode;

using UnityEngine;

namespace WeaponHandling
{
    public sealed class WeaponKickback : NetworkBehaviour
    {

        [SerializeField] private Transform weaponTransform; // eventually add a new layer 
        [SerializeField] private Vector3 basePosition;

        private WeaponHandler weaponHandler;
        private KickbackStats KickbackStats => weaponHandler.CurrentWeapon.KickbackStats;

        public void SetupData(WeaponHandler weapnHandler_)
        {
            weaponHandler = weapnHandler_;
        }



        [Rpc(SendTo.Server)]
        public void ApplyKickbackServerRpc(float chargeRatio = 1f)
        {
            ApplyKickbackClientRpc(chargeRatio);
        }

        public void ApplyKickbackFromServer(float chargeRatio = 1f)
        {
            ApplyKickbackClientRpc(chargeRatio);
        }

        [Rpc(SendTo.ClientsAndHost)]
        private void ApplyKickbackClientRpc(float chargeRatio)
        {
            weaponTransform.localPosition -= new Vector3(0f, 0f, KickbackStats.WeaponKickBackPerShot * chargeRatio);
        }

        [Rpc(SendTo.Server)]
        public void HandleKickbackServerRpc()
        {
            HandleKickbackClientRpc();
        }

        [Rpc(SendTo.ClientsAndHost)]
        private void HandleKickbackClientRpc()
        {
            weaponTransform.localPosition = Vector3.Slerp(weaponTransform.localPosition, basePosition, KickbackStats.WeaponKickBackRegulationTime * Time.deltaTime);
        }
    }
}