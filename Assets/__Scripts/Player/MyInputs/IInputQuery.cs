using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;

namespace Inputs
{
    public interface IInputQuery
    {
        public List<Keybind> Keybinds { get; }
        public bool IsRebindingAKey { get; }

        public void RegisterKeybind(Keybind bind);

        public void Init();

        public void OnRenderRebindMenu();



#if false
        public void RegisterKeybind(Keybind keybind)
        {
            Keybinds.Add(keybind);
            keybind.Init();
        }

        public void OnRenderRebindMenu()
        {
            IsRebindingAKey = false;

            foreach (var keybind in Keybinds)
            {
                IsRebindingAKey |= keybind.OnRenderRebindMenu();
            }
        }
#endif
    }
}