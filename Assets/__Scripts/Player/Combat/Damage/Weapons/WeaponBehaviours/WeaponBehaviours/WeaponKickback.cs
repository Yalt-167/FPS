using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using MyEditorUtilities;

namespace WeaponHandling
{
    public sealed class WeaponKickback : WeaponBehaviour
    {
        public override void Setup(Weapon weapon)
        {
            throw new NotImplementedException();
        }

        [SerializeField] private Transform weaponTransform; // not sustainable for dual wielding weapons
        private KickbackStats kickbackStats => weaponHandler.CurrentWeapon.KickbackStats;
        private WeaponHandler weaponHandler;

        private void Awake()
        {
            Setup();
            weaponHandler = GetComponent<WeaponHandler>();
            weaponTransform = transform.GetChild(0).GetChild(0).GetChild(0).GetChild(0).GetChild(0).GetChild(0);
        }

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