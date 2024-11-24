using System;
using System.Collections;
using System.Collections.Generic;

using Unity.Netcode;

using UnityEngine;

namespace WeaponHandling
{
    public sealed class PlayerWeaponsGathererNetworked : INetworkSerializable, IEquatable<PlayerWeaponsGathererNetworked>
    {
        public WeaponRuntimeData[] Weapons;

        public PlayerWeaponsGathererNetworked()
        {
            Weapons = new WeaponRuntimeData[0] { };
        }
        public PlayerWeaponsGathererNetworked(PlayerWeaponsGathererStatic playeWeaponGathererStatic)
        {
            Weapons = new WeaponRuntimeData[playeWeaponGathererStatic.WeaponCount];
            for (int i = 0; i < playeWeaponGathererStatic.WeaponCount; i++)
            {
                Weapons[i] = new WeaponRuntimeData(
                    playeWeaponGathererStatic[i].MagazineSize
                );       
            }
        }

        public WeaponRuntimeData this[int index]
        {
            get
            {
                return Weapons[index];
            }
        }

        public bool Equals(PlayerWeaponsGathererNetworked other)
        {
            if (Weapons.Length != other.Weapons.Length) {  return false; }

            for (int i = 0; i < Weapons.Length; i++)
            {
                if (Weapons[i] != other.Weapons[i]) {  return false; }
            }


            return true;
        }

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            for (int i = 0; i < Weapons.Length; i++)
            {
                Weapons[i].NetworkSerialize(serializer);
            }
        }
    }
}