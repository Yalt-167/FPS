using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;

namespace Inputs
{
    [Serializable]
    public abstract class InputQuery
    {
        protected List<Keybind> keybinds = new();
        public bool IsRebindingAKey { get; protected set; }
        public bool InputDisabled { get; protected set; }
        public abstract void Init();

        public void RegisterKeybind(Keybind keybind)
        {
            keybinds.Add(keybind);
            keybind.Init();
        }

        public void OnRenderRebindMenu()
        {
            IsRebindingAKey = false;

            foreach (var keybind in keybinds)
            {
                IsRebindingAKey |= keybind.OnRenderRebindMenu();
            }
        }
    }
}