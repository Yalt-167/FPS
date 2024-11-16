using System;
using System.Collections;
using System.Collections.Generic;

using Unity.VisualScripting;

using UnityEngine;
using UnityEngine.Rendering;

namespace WeaponHandling
{
    public struct PlayerWeaponGatherer
    {
        public int WeaponCount;
        public WeaponRuntimeData[] weapons;

        public PlayerWeaponGatherer(string[] SO_Paths)
        {
            WeaponCount = SO_Paths.Length;
            weapons = new WeaponRuntimeData[WeaponCount];
            for (int i = 0; i < WeaponCount; i++)
            {
                weapons[i] = new WeaponRuntimeData(MyUtilities.AssetLoader.LoadAsset<WeaponScriptableObject>(SO_Paths[i]));       
            }
        }

        public readonly WeaponRuntimeData this[int index]
        {
            get
            {
                return weapons[index];
            }
        }
    }
}