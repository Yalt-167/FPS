using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;

namespace Inputs
{
    [Serializable]
    public sealed class GeneralInputQuery : IInputQuery
    {
        public FixedKeybind TogglePauseMenu = new(KeyCode.Escape, InputType.OnKeyDown, nameof(TogglePauseMenu), false);
        public VariableKeybind ShowScoreboard = new(KeyCode.Tab, new List<InputType>() { InputType.Toggle, InputType.OnKeyHeld }, nameof(ShowScoreboard), true);

        public List<Keybind> Keybinds { get; private set; } = new();
        public bool IsRebindingAKey { get; private set; }


        public void Init()
        {
            RegisterKeybind(TogglePauseMenu);
            RegisterKeybind(ShowScoreboard);
        }

        public void OnRenderRebindMenu()
        {
            IsRebindingAKey = false;

            foreach (var keybind in Keybinds)
            {
                IsRebindingAKey |= keybind.OnRenderRebindMenu();
            }
        }

        public void RegisterKeybind(Keybind keybind)
        {
            Keybinds.Add(keybind);
            keybind.Init();
        }
    }
}