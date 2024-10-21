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
        private NetworkVariable<KickbackStats> kickbackStats = new NetworkVariable<KickbackStats>(readPerm: NetworkVariableReadPermission.Everyone, writePerm: NetworkVariableWritePermission.Server);
        [SerializeField] private Vector3 basePosition;


        public IEnumerator SetData(KickbackStats kickbackStats_)
        {
            yield return new WaitUntil(() => IsSpawned);

            SetDataServerRpc(kickbackStats_);
        }

        [Rpc(SendTo.Server)]
        public void SetDataServerRpc(KickbackStats kickbackStats_)
        {
            kickbackStats.Value = kickbackStats_;
        }



        [Rpc(SendTo.Server)]
        public void ApplyKickbackServerRpc(float chargeRatio = 1f)
        {
            ApplyKickbackClientRpc(chargeRatio);
        }
        
        [Rpc(SendTo.ClientsAndHost)]
        private void ApplyKickbackClientRpc(float chargeRatio)
        {
            weaponTransform.localPosition -= new Vector3(0f, 0f, kickbackStats.Value.WeaponKickBackPerShot * chargeRatio);
        }

        [Rpc(SendTo.Server)]
        public void HandleKickbackServerRpc()
        {
            HandleKickbackClientRpc();
        }

        [Rpc(SendTo.ClientsAndHost)]
        private void HandleKickbackClientRpc()
        {
            weaponTransform.localPosition = Vector3.Slerp(weaponTransform.localPosition, basePosition, kickbackStats.Value.WeaponKickBackRegulationTime * Time.time);
        }
    }
}