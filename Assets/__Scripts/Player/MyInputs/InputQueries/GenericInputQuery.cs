using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;

namespace Inputs
{
    [Serializable]
    public sealed class GenericInputQuery : IInputQuery
    {
        public FixedKeybind TogglePauseMenu = new(KeyCode.Escape, PlayerInputType.OnKeyDown, nameof(TogglePauseMenu));
        public GroupKeybind TogglePauseMenu2 = new(KeyCode.Escape, new PlayerInputType[] { PlayerInputType.OnKeyDown , PlayerInputType.OnKeyUp}, new string[] { "Initiate", "Stop"}, nameof(TogglePauseMenu));

        public List<Keybind> Keybinds { get; private set; } = new();

        public bool DoRenderRebindMenu { get; private set; }

        public void Init()
        {
            TogglePauseMenu.Init(this);
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
        }
    }
}