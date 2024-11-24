using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;

namespace Inputs
{
    [Serializable]
    public sealed class CombatInputQuery : InputQuery
    {
        public FixedKeybind Fire = new(KeyCode.Mouse0, InputType.OnKeyHeld, nameof(Fire), true);
        public VariableKeybind Aim = new(KeyCode.Mouse1, new List<InputType>() { InputType.OnKeyHeld, InputType.Toggle }, nameof(Aim), true);
        public FixedKeybind Reload = new(KeyCode.R, InputType.OnKeyDown, nameof(Reload), true);

        public FixedKeybind FirstGun = new(KeyCode.Alpha1, InputType.OnKeyDown, nameof(FirstGun), true);
        public FixedKeybind SecondGun = new(KeyCode.Alpha2, InputType.OnKeyDown, nameof(SecondGun), true);
        public FixedKeybind ThirdGun = new(KeyCode.Alpha3, InputType.OnKeyDown, nameof(ThirdGun), true);

        public GroupKeybind PrimaryAbility = new(KeyCode.None, new InputType[] { InputType.OnKeyDown, InputType.OnKeyUp }, nameof(PrimaryAbility), true);
        public GroupKeybind SecondaryAbility = new(KeyCode.None, new InputType[] { InputType.OnKeyDown, InputType.OnKeyUp }, nameof(SecondaryAbility), true);
        public GroupKeybind Ultimate = new(KeyCode.None, new InputType[] { InputType.OnKeyDown, InputType.OnKeyUp }, nameof(Ultimate), true);

        public FixedKeybind Slash = new(KeyCode.None, InputType.OnKeyDown, nameof(Slash), true);
        public FixedKeybind Parry = new(KeyCode.None, InputType.OnKeyDown, nameof(Parry), true);

        public override void Init()
        {
            RegisterKeybind(Fire);
            RegisterKeybind(Aim);
            RegisterKeybind(Reload);

            RegisterKeybind(FirstGun);
            RegisterKeybind(SecondGun);
            RegisterKeybind(ThirdGun);

            RegisterKeybind(PrimaryAbility);
            RegisterKeybind(SecondaryAbility);
            RegisterKeybind(Ultimate);

            RegisterKeybind(Slash);
            RegisterKeybind(Parry);
        }
    }
}