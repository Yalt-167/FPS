using System;
using System.Collections;
using System.Collections.Generic;

using Unity.Netcode;

using UnityEngine;

namespace WeaponHandling
{
    public class WeaponRuntimeData : INetworkSerializable
    {
        public int Ammos;
        public float TimeLastShotFired;

        public WeaponRuntimeData(int magazineSize)
        {
            Ammos = magazineSize;
            TimeLastShotFired = float.MinValue;
        }

        public void Shoot(bool isServer)
        {
            if (!isServer)
            {
                Debug.Log("WeaponRuntimeData.Shoot() called from a client");
                return;
            }

            Debug.Log("Been there");

            TimeLastShotFired = Time.time;
            Ammos--;
        }

        public void Reload(int magazineSize, bool isServer)
        {
            if (!isServer)
            {
                Debug.Log("WeaponRuntimeData.Reload() called from a client");
                return;
            }

            Ammos = magazineSize;
        }

        public void ReloadOne(bool isServer)
        {
            if (!isServer)
            {
                Debug.Log("WeaponRuntimeData.ReloadOne() called from a client");
                return;
            }

            Ammos++;
        }

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref Ammos);
            serializer.SerializeValue(ref TimeLastShotFired);
        }
    }
}