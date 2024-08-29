using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;

namespace Inputs
{
    public sealed class GenericInputQuery : IInputQuery
    {
        public FixedKeybind TogglePauseMenu = new(KeyCode.Escape, PlayerActionActivationType.OnKeyDown);

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
                keybind.OnRenderRebingMenu();
            }
        }

        public void RegisterKeybind(Keybind bind)
        {
            Keybinds.Add(bind);
        }
    }
}