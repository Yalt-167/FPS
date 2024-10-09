using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using Unity.Netcode;


namespace WeaponHandling
{
    public abstract class Weapon : Holdable
    {
        [SerializeField] private WeaponScriptableObject weaponData;

        private WeaponRecoil weaponRecoil;
        private WeaponBehaviourGatherer<WeaponADS> weaponADSGatherer;
        private WeaponBehaviourGatherer<WeaponSpread> weaponSpreadGatherer;
        private WeaponBehaviourGatherer<WeaponKickback> weaponKickbackGatherer;
        private WeaponBehaviourGatherer<BarrelEnd> barrelEndsGatherer;

        private ShootingStrategy shootingStrategy;

        protected virtual void Awake()
        {
            weaponRecoil = GetComponentInChildren<WeaponRecoil>();
            weaponADSGatherer = new(GetComponentsInChildren<WeaponADS>());
            weaponSpreadGatherer = new(GetComponentsInChildren<WeaponSpread>());
            weaponKickbackGatherer = new(GetComponentsInChildren<WeaponKickback>());
            barrelEndsGatherer = new(GetComponentsInChildren<BarrelEnd>());
        }

        protected void SetShootingStrategy()
        {

        }


        //public override void OnPullOut()
        //{
        //    base.OnPullOut();
        //}

        //public override void OnPutAway()
        //{
        //    base.OnPutAway();
        //}


        //public override void OnPrimaryUseKeyDown()
        //{
        //    base.OnPrimaryUseKeyDown();
        //}


        //public override void OnPrimaryUseKeyUp()
        //{
        //    base.OnPrimaryUseKeyUp();
        //}


    }
}