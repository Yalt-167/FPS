using System;
using System.Collections;
using System.Collections.Generic;

using UnityEditor;

using UnityEngine;

namespace WeaponHandling
{
    public abstract class WeaponBehaviour : MonoBehaviour
    {
        // public abstract void InitWeapon();

        // public abstract void SwitchToWeapon();

        // public abstract void SwitchOffWeapon();

        public abstract void Setup(Weapon weapon);
    }
}