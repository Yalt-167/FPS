using System;
using System.Collections;
using System.Collections.Generic;

using Unity.Netcode;
using Unity.VisualScripting;

using UnityEngine;

namespace WeaponHandling
{
    public struct PlayerWeaponsGathererNetworked : INetworkSerializable
    {
        public WeaponRuntimeData[] weapons;

        public PlayerWeaponsGathererNetworked(PlayerWeaponsGathererStatic playeWeaponGathererStatic)
        {
            weapons = new WeaponRuntimeData[playeWeaponGathererStatic.WeaponCount];
            for (int i = 0; i < playeWeaponGathererStatic.WeaponCount; i++)
            {
                weapons[i] = new WeaponRuntimeData(
                    playeWeaponGathererStatic[i].MagazineSize
                );       
            }
        }

        public readonly WeaponRuntimeData this[int index]
        {
            get
            {
                return weapons[index];
            }
        }

        public readonly void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            for (int i = 0; i < weapons.Length; i++)
            {
                weapons[i].NetworkSerialize(serializer);
            }
        }
    }

    public struct PlayerWeaponsGathererStatic
    {
        public static PlayerWeaponsGathererStatic Empty = new(new string[0] { });
        public int WeaponCount;
        public WeaponScriptableObject[] Weapons;
        public PlayerWeaponsGathererStatic(string[] SO_Paths)
        {
            WeaponCount = SO_Paths.Length;
            Weapons = new WeaponScriptableObject[WeaponCount];
            for (int i = 0; i < WeaponCount; i++)
            {
                Weapons[i] = MyUtilities.AssetLoader.LoadAsset<WeaponScriptableObject>(SO_Paths[i]);
            }

        }

        public readonly WeaponScriptableObject this[int index]
        {
            get
            {
                return Weapons[index];
            }
        }

        public override readonly bool Equals(object obj)
        {
            return obj is PlayerWeaponsGathererStatic other &&
                   WeaponCount == other.WeaponCount &&
                   EqualityComparer<WeaponScriptableObject[]>.Default.Equals(Weapons, other.Weapons);
        }

        public override readonly int GetHashCode()
        {
            return HashCode.Combine(WeaponCount, Weapons);
        }

        public static bool operator==(PlayerWeaponsGathererStatic self, PlayerWeaponsGathererStatic other)
        {
            if (self.WeaponCount != other.WeaponCount) { return false; }

            for (int i = 0; i < self.WeaponCount; i++)
            {
                if (self.Weapons[i] != other.Weapons[i]) { return false; }
            }

            return true;
        }

        public static bool operator !=(PlayerWeaponsGathererStatic self, PlayerWeaponsGathererStatic other)
        {
            return !(self == other);
        }
    }
}