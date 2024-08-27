using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;

namespace Inputs
{
    [Serializable]
    public sealed class CombatInputQuery : InputQuery
    {
        public FixedKeybind Shoot = new(KeyCode.Mouse0, PlayerActionActivationType.OnKeyHeld);
        public VariableKeybind Aim = new(KeyCode.Mouse1, new() { PlayerActionActivationType.OnKeyHeld, PlayerActionActivationType.Toggle });
        public FixedKeybind Reload = new(KeyCode.R, PlayerActionActivationType.OnKeyDown);

        public FixedKeybind FirstGun = new(KeyCode.Alpha1, PlayerActionActivationType.OnKeyDown);
        public FixedKeybind SecondGun = new(KeyCode.Alpha2, PlayerActionActivationType.OnKeyDown);
        public FixedKeybind ThirdGun = new(KeyCode.Alpha3, PlayerActionActivationType.OnKeyDown);

        public FixedKeybind PrimaryAbility;
        public FixedKeybind SecondaryAbility;
        public FixedKeybind Ultimate;


        public FixedKeybind Slash;
        public FixedKeybind Parry;
        public FixedKeybind ChangeCrosshair;


        public override void Init()
        {
            Shoot.Init();
            Aim.Init();
            Reload.Init();

            //NextWeapon.Init();
            //PreviousWeapon.Init();

            FirstGun.Init();
            SecondGun.Init();
            ThirdGun.Init();

            PrimaryAbility.Init();
            SecondaryAbility.Init();
            Ultimate.Init();

            Slash.Init();
            Parry.Init();

            ChangeCrosshair.Init();
        }
    }

}