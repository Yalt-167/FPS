using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;

namespace WeaponHandling
{
    public struct WeaponRuntimeData
    {
        public WeaponScriptableObject Weapon;
        public int Ammo;
        public float LastShotFired;

        public WeaponRuntimeData(WeaponScriptableObject weapon)
        {
            Weapon = weapon;
            Ammo = weapon.MagazineSize;
            LastShotFired = float.MinValue;
        }
    }
}