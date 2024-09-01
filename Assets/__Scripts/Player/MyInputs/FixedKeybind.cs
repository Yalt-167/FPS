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
            inputType = activationType;
            inputTypeAsStr = activationType.ToString();
            name = name_;
        }

        public FixedKeybind(KeyCode relevantKey, PlayerInputType activationType, float _holdForSeconds, string name_)
        {
            RelevantKey = relevantKey;
            relevantKeyAsStr = relevantKey.ToString();
            inputType = activationType;
            inputTypeAsStr = activationType.ToString();
            holdForSeconds = _holdForSeconds;
            name = name_;
        }

        public override void OnRenderRebindMenu()
        {
            GUILayout.BeginHorizontal(CachedGUIStylesNames.Box);

            DisplayCurrentKey();

            GUILayout.EndHorizontal();
        }
    }
}