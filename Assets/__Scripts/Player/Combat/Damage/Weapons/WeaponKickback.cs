using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;

namespace WeaponHandling
{
    public sealed class WeaponKickback : MonoBehaviour
    {
        [SerializeField] private Transform weaponTransform;
        private KickbackStats kickbackStats;
        //private WeaponStats weaponStats; // not sure which one I wanna use so far

        private void ApplyKickback(float chargeRatio = 1f)
        {
            weaponTransform.localPosition -= new Vector3(0f, 0f, kickbackStats.WeaponKickBackPerShot * chargeRatio);
        }

        private void HandleKickback()
        {
            weaponTransform.localPosition = Vector3.Slerp(weaponTransform.localPosition, Vector3.zero, kickbackStats.WeaponKickBackRegulationTime * Time.time);
        }

        private void Update()
        {
            HandleKickback();
        }
    }
}