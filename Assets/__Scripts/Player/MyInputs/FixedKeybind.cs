using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;

namespace Inputs
{
    [Serializable]
    public sealed class FixedKeybind : Keybind
    {
        public FixedKeybind(KeyCode relevantKey, PlayerActionActivationType activationTypes)
        {
            RelevantKey = relevantKey;
            howToActivate = activationTypes;
        }

        public FixedKeybind(KeyCode relevantKey, PlayerActionActivationType activationTypes, float _holdForSeconds)
        {
            RelevantKey = relevantKey;
            howToActivate = activationTypes;
            holdForSeconds = _holdForSeconds;
        }
    }
}