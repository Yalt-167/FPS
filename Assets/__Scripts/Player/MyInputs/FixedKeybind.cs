using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;

namespace Inputs
{
    [Serializable]
    public sealed class FixedKeybind : Keybind
    {
        public FixedKeybind(KeyCode relevantKey, PlayerInputType activationType, string name_)
        {
            RelevantKey = relevantKey;
            relevantKeyAsStr = relevantKey.ToString();
            howToActivate = activationType;
            howToActivateAsStr = activationType.ToString();
            name = name_;
        }

        public FixedKeybind(KeyCode relevantKey, PlayerInputType activationType, float _holdForSeconds, string name_)
        {
            RelevantKey = relevantKey;
            relevantKeyAsStr = relevantKey.ToString();
            howToActivate = activationType;
            howToActivateAsStr = activationType.ToString();
            holdForSeconds = _holdForSeconds;
            name = name_;
        }

        public override void OnRenderRebindMenu()
        {
            GUILayout.BeginHorizontal(CachedGUIStylesNames.Box);

            GUILayout.Label(name);
            GUILayout.BeginHorizontal(CachedGUIStylesNames.Box);
            GUILayout.Label(relevantKeyAsStr);
            GUILayout.EndHorizontal();

            GUI.enabled = false;
            GUILayout.BeginHorizontal(CachedGUIStylesNames.Box);
            GUILayout.Label(howToActivateAsStr);
            GUILayout.EndHorizontal();
            GUI.enabled = true;

            GUILayout.EndHorizontal();
        }
    }
}