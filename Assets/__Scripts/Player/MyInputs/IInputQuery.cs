using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;

namespace Inputs
{
    public interface IInputQuery
    {
        public List<Keybind> Keybinds { get; }

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
            foreach (var keybind in Keybinds)
            {
                keybind.OnRenderRebindMenu();
            }
        }
#endif
    }
}