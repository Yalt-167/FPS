using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;

namespace Inputs
{
    [Serializable]
    public sealed class FixedKeybind : Keybind
    {
        public FixedKeybind(KeyCode relevantKey, InputType inputType_, string name_, bool canBeRemapped_, float _holdForSeconds = 0.0f)
        {
            RelevantKey = relevantKey;
            relevantKeyAsStr = relevantKey.ToString();
            inputType = inputType_;
            inputTypeAsStr = inputType_.ToString();
            name = name_;
            canBeRemapped = canBeRemapped_;
            holdForSeconds = _holdForSeconds;
        }

        public override bool OnRenderRebindMenu()
        {
            GUILayout.BeginHorizontal(CachedGUIStylesNames.Box);

            var isRemappingAKey = DisplayCurrentKey();

            GUILayout.EndHorizontal();

            return isRemappingAKey;
        }
    }
}