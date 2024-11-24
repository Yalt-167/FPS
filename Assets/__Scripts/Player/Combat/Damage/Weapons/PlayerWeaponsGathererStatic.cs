using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;

namespace WeaponHandling
{
    public sealed class PlayerWeaponsGathererStatic
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

        public WeaponScriptableObject this[int index]
        {
            get
            {
                return Weapons[index];
            }
        }

        public override bool Equals(object obj)
        {
            return obj is PlayerWeaponsGathererStatic other &&
                   WeaponCount == other.WeaponCount &&
                   EqualityComparer<WeaponScriptableObject[]>.Default.Equals(Weapons, other.Weapons);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(WeaponCount, Weapons);
        }

        public static bool operator ==(PlayerWeaponsGathererStatic self, PlayerWeaponsGathererStatic other)
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