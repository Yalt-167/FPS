using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;

namespace Inputs
{
    [Serializable]
    public sealed class GeneralInputQuery : InputQuery
    {
        public FixedKeybind TogglePauseMenu = new(KeyCode.Escape, InputType.OnKeyDown, nameof(TogglePauseMenu), false);
        public VariableKeybind ShowScoreboard = new(KeyCode.Tab, new List<InputType>() { InputType.Toggle, InputType.OnKeyHeld }, nameof(ShowScoreboard), true);

        public override void Init()
        {
            RegisterKeybind(TogglePauseMenu);
            RegisterKeybind(ShowScoreboard);
        }
    }
}