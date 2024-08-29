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

        public override void OnRenderRebindMenu()
        {
            GUILayout.BeginHorizontal(CachedGUIStylesNames.Box);

            GUILayout.Label(name);
            GUILayout.BeginHorizontal(CachedGUIStylesNames.Box);
            GUILayout.Label(RelevantKey.ToString()); // eventually store that as a memeber variable to avoid unecessary garbage collection
            GUILayout.EndHorizontal();

            GUI.enabled = false;
            GUILayout.BeginHorizontal(CachedGUIStylesNames.Box);
            GUILayout.Label(howToActivate.ToString()); // eventually store that as a memeber variable to avoid unecessary garbage collection
            GUILayout.EndHorizontal();
            GUI.enabled = true;

            GUILayout.EndHorizontal();
        }
    }
}