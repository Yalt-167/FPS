using System;
using System.Collections;
using System.Collections.Generic;

using Unity.Netcode;

using UnityEngine;

namespace WeaponHandling
{
    public abstract class ShootingStrategy : NetworkBehaviour
    {
        [Rpc(SendTo.Server)]
        public virtual void ShootServerRpc() { }


        [Rpc(SendTo.ClientsAndHost)]
        public virtual void ShootClientRpc() { }
    }
}