using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;

namespace Inputs
{
    [Serializable]
    public sealed class FixedKeybind : Keybind
    {
        public FixedKeybind(KeyCode relevantKey, PlayerActionActivationType activationTypes, string name_)
        {
            RelevantKey = relevantKey;
            howToActivate = activationTypes;
            name = name_;
        }

        public FixedKeybind(KeyCode relevantKey, PlayerActionActivationType activationTypes, float _holdForSeconds, string name_)
        {
            RelevantKey = relevantKey;
            howToActivate = activationTypes;
            holdForSeconds = _holdForSeconds;
            name = name_;
        }

        public override void OnRenderRebingMenu()
        {
            GUILayout.BeginHorizontal(CachedGUIStylesNames.Box);


            GUILayout.EndHorizontal();
        }
    }
}