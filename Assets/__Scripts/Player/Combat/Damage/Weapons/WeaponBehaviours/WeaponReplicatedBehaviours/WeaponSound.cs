using System;
using System.Collections;
using System.Collections.Generic;

using Unity.Netcode;

using UnityEngine;

namespace WeaponHandling
{
    public sealed class WeaponSound : WeaponReplicatedBehaviour 
    {
        [Rpc(SendTo.Server)]
        public void PlaySoundServerRpc()
        {
            PlaySoundClientRpc();
        }

        [Rpc(SendTo.ClientsAndHost)]
        private void PlaySoundClientRpc()
        {

        }
    }
}