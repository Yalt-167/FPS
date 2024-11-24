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

        public WeaponRuntimeData()
        {
            Ammos = 0;
            TimeLastShotFired = float.MinValue;
        }
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

        public override bool Equals(object obj)
        {
            return obj is WeaponRuntimeData data &&
                   Ammos == data.Ammos &&
                   TimeLastShotFired == data.TimeLastShotFired;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Ammos, TimeLastShotFired);
        }

        public static bool operator==(WeaponRuntimeData self, WeaponRuntimeData other)
        {
            return self.Ammos == other.Ammos && self.TimeLastShotFired == other.TimeLastShotFired;
        }

        public static bool operator !=(WeaponRuntimeData self, WeaponRuntimeData other)
        {
            return !(self == other);
        }
    }
}