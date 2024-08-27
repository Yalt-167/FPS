using Inputs;
using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;

namespace Inputs
{
    [Serializable]
    public sealed class VariableKeybind : Keybind
    {
        [SerializeField] private List<PlayerActionActivationType> allowedActivationTypes;


        private int activationTypeIndex;
        private int activationTypesLength;

        /// <summary>
        /// First provided activation type is gonna be selected as default
        /// </summary>
        /// <param name="relevantKey"></param>
        /// <param name="_allowedActivationTypes"></param>
        public VariableKeybind(KeyCode relevantKey, List<PlayerActionActivationType> _allowedActivationTypes)
        {
            RelevantKey = relevantKey;
            allowedActivationTypes = _allowedActivationTypes;
            howToActivate = allowedActivationTypes[0];
        }

        public VariableKeybind(KeyCode relevantKey, List<PlayerActionActivationType> _allowedActivationTypes, float _holdForSeconds)
        {
            RelevantKey = relevantKey;
            allowedActivationTypes = _allowedActivationTypes;
            howToActivate = allowedActivationTypes[0];
            holdForSeconds = _holdForSeconds;
        }

        public new void Init()
        {
            base.Init();
            activationTypeIndex = allowedActivationTypes.IndexOf(howToActivate);
            activationTypesLength = allowedActivationTypes.Count;
        }

        public void SetActivationType(PlayerActionActivationType newActivationType)
        {
            howToActivate = newActivationType;
            SetRelevantOutputSettings();
        }

        public void NextActivationType()
        {
            howToActivate = allowedActivationTypes[++activationTypeIndex % activationTypesLength];
            SetRelevantOutputSettings();
        }
    }
}