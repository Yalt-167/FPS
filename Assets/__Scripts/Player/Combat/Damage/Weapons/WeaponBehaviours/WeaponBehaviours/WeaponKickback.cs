using System;
using System.Collections;
using System.Collections.Generic;

using Unity.Netcode;

using UnityEngine;

namespace WeaponHandling
{
    public sealed class WeaponKickback : NetworkBehaviour
    {

        [SerializeField] private Transform weaponSocketTransform;
        private NetworkVariable<KickbackStats> kickbackStats = new NetworkVariable<KickbackStats>(readPerm: NetworkVariableReadPermission.Everyone, writePerm: NetworkVariableWritePermission.Server);
        private bool hasSpawnedOnNetwork;

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            hasSpawnedOnNetwork = true;
        }

        public IEnumerator SetData(KickbackStats kickbackStats_)
        {
            yield return new WaitUntil(() => hasSpawnedOnNetwork);

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
            weaponSocketTransform.localPosition -= new Vector3(0f, 0f, kickbackStats.Value.WeaponKickBackPerShot * chargeRatio);
        }

        [Rpc(SendTo.Server)]
        public void HandleKickbackServerRpc()
        {
            HandleKickbackClientRpc();
        }

        [Rpc(SendTo.ClientsAndHost)]
        private void HandleKickbackClientRpc()
        {
            weaponSocketTransform.localPosition = Vector3.Slerp(weaponSocketTransform.localPosition, Vector3.zero, kickbackStats.Value.WeaponKickBackRegulationTime * Time.time);
        }
    }
}