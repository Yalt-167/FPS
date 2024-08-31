using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;

namespace Inputs
{
    [Serializable]
    public sealed class CombatInputQuery : IInputQuery
    {
        public FixedKeybind Shoot = new(KeyCode.Mouse0, PlayerInputType.OnKeyHeld, nameof(Shoot));
        public VariableKeybind Aim = new(KeyCode.Mouse1, new() { PlayerInputType.OnKeyHeld, PlayerInputType.Toggle }, nameof(Aim));
        public FixedKeybind Reload = new(KeyCode.R, PlayerInputType.OnKeyDown, nameof(Reload));

        public FixedKeybind FirstGun = new(KeyCode.Alpha1, PlayerInputType.OnKeyDown, nameof(FirstGun));
        public FixedKeybind SecondGun = new(KeyCode.Alpha2, PlayerInputType.OnKeyDown, nameof(SecondGun));
        public FixedKeybind ThirdGun = new(KeyCode.Alpha3, PlayerInputType.OnKeyDown, nameof(ThirdGun));

        public GroupKeybind PrimaryAbility = new(KeyCode.None, new Dictionary<string, PlayerInputType>() { { "Initiate", PlayerInputType.OnKeyDown }, { "Release", PlayerInputType.OnKeyUp } }, nameof(PrimaryAbility));
        public GroupKeybind SecondaryAbility = new(KeyCode.None, new Dictionary<string, PlayerInputType>() { { "Initiate", PlayerInputType.OnKeyDown }, { "Release", PlayerInputType.OnKeyUp } }, nameof(SecondaryAbility));
        public GroupKeybind Ultimate = new(KeyCode.None, new Dictionary<string, PlayerInputType>() { { "Initiate", PlayerInputType.OnKeyDown }, { "Release", PlayerInputType.OnKeyUp } }, nameof(Ultimate));

        public FixedKeybind Slash;
        public FixedKeybind Parry;

        public List<Keybind> Keybinds { get; private set; } = new();
        public bool DoRenderRebindMenu { get; private set; }

        public void Init()
        {
            RegisterKeybind(Shoot);
            RegisterKeybind(Aim);
            RegisterKeybind(Reload);

            //NextWeapon.Init();
            //PreviousWeapon.Init();

            RegisterKeybind(FirstGun);
            RegisterKeybind(SecondGun);
            RegisterKeybind(ThirdGun);

            RegisterKeybind(PrimaryAbility);
            RegisterKeybind(SecondaryAbility);
            RegisterKeybind(Ultimate);

            RegisterKeybind(Slash);
            RegisterKeybind(Parry);
        }

        public void OnRenderRebindMenu()
        {
            foreach (var keybind in Keybinds)
            {
                keybind.OnRenderRebindMenu();
            }
        }

        public void RegisterKeybind(Keybind bind)
        {
            Keybinds.Add(bind);
            bind.Init();
        }
    }
}