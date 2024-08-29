using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;

namespace Inputs
{
    [Serializable]
    public sealed class CombatInputQuery : IInputQuery
    {
        public FixedKeybind Shoot = new(KeyCode.Mouse0, PlayerActionActivationType.OnKeyHeld);
        public VariableKeybind Aim = new(KeyCode.Mouse1, new() { PlayerActionActivationType.OnKeyHeld, PlayerActionActivationType.Toggle });
        public FixedKeybind Reload = new(KeyCode.R, PlayerActionActivationType.OnKeyDown);


        public FixedKeybind FirstGun = new(KeyCode.Alpha1, PlayerActionActivationType.OnKeyDown);
        public FixedKeybind SecondGun = new(KeyCode.Alpha2, PlayerActionActivationType.OnKeyDown);
        public FixedKeybind ThirdGun = new(KeyCode.Alpha3, PlayerActionActivationType.OnKeyDown);


        public FixedKeybind InitiatePrimaryAbility;
        public FixedKeybind ReleasePrimaryAbility;
        public FixedKeybind InitiateSecondaryAbility;
        public FixedKeybind ReleaseSecondaryAbility;
        public FixedKeybind InitiateUltimate;
        public FixedKeybind ReleaseUltimate;


        public FixedKeybind Slash;
        public FixedKeybind Parry;

        public List<Keybind> Keybinds { get; private set; } = new();
        public bool DoRenderRebindMenu { get; private set; }

        public void Init()
        {
            Shoot.Init(this);
            Aim.Init(this);
            Reload.Init(this);

            //NextWeapon.Init();
            //PreviousWeapon.Init();

            FirstGun.Init(this);
            SecondGun.Init(this);
            ThirdGun.Init(this);

            InitiatePrimaryAbility.Init(this);
            ReleasePrimaryAbility.Init(this);
            InitiateSecondaryAbility.Init(this);
            ReleaseSecondaryAbility.Init(this);
            InitiateUltimate.Init(this);
            ReleaseUltimate.Init(this);

            Slash.Init(this);
            Parry.Init(this);
        }

        public void OnRenderRebindMenu()
        {
            foreach (var keybind in Keybinds)
            {
                keybind.OnRenderRebingMenu();
            }
        }

        public void RegisterKeybind(Keybind bind)
        {
            Keybinds.Add(bind);
        }
    }
}