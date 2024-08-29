using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;

namespace Inputs
{
    public interface IInputQuery
    {
        public List<Keybind> Keybinds { get; }
        public bool DoRenderRebindMenu { get; }

        public void RegisterKeybind(Keybind bind);

#if false
        public void RegisterKeybind(Keybind bind)
        {
            Keybinds.Add(bind);
        }
#endif


        public void Init();

        public void OnRenderRebindMenu();

#if false
public void OnGUI_Internal()
        {
            foreach (var keybind in Keybinds)
            {
                keybind.OnGUI_Internal();
            }
        }
#endif
    }
}