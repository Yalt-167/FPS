using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;

namespace Inputs
{
    public sealed class GenericInputQuery : IInputQuery
    {
        public FixedKeybind TogglePauseMenu = new(KeyCode.Escape, PlayerActionActivationType.OnKeyDown, nameof(TogglePauseMenu));

        public List<Keybind> Keybinds { get; private set; }

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