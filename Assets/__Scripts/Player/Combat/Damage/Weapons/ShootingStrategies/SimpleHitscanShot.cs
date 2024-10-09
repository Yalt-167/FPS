using System;
using System.Collections;
using System.Collections.Generic;

using Unity.Netcode;

using UnityEngine;

namespace WeaponHandling
{
    public sealed class SimpleHitscanShot : ShootingStrategy
    {
        [Rpc(SendTo.Server)]
        public override void ShootServerRpc()
        {

        }


        [Rpc(SendTo.ClientsAndHost)]
        public override void ShootClientRpc() { }
    }
}