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
        [SerializeField] private List<PlayerInputType> allowedActivationTypes;


        private int activationTypeIndex;
        private int activationTypesLength;

        /// <summary>
        /// First provided activation type is gonna be selected as default
        /// </summary>
        /// <param name="relevantKey"></param>
        /// <param name="_allowedActivationTypes"></param>
        public VariableKeybind(KeyCode relevantKey, List<PlayerInputType> _allowedActivationTypes, string name_)
        {
            RelevantKey = relevantKey;
            relevantKeyAsStr = relevantKey.ToString();
            allowedActivationTypes = _allowedActivationTypes;
            inputType = allowedActivationTypes[0];
            inputTypeAsStr = inputType.ToString();
            name = name_;
        }

        public VariableKeybind(KeyCode relevantKey, List<PlayerInputType> _allowedActivationTypes, float _holdForSeconds, string name_)
        {
            RelevantKey = relevantKey;
            relevantKeyAsStr = relevantKey.ToString();
            allowedActivationTypes = _allowedActivationTypes;
            inputType = allowedActivationTypes[0];
            inputTypeAsStr = inputType.ToString();
            holdForSeconds = _holdForSeconds;
            name = name_;
        }

        public override void Init()
        {
            base.Init();
            activationTypeIndex = allowedActivationTypes.IndexOf(inputType);
            activationTypesLength = allowedActivationTypes.Count;
        }

        public void SetActivationType(PlayerInputType newActivationType)
        {
            inputType = newActivationType;
            SetRelevantOutputSettings();
        }

        public void NextActivationType()
        {
            inputType = allowedActivationTypes[++activationTypeIndex % activationTypesLength];
            SetRelevantOutputSettings();
        }

        public override void OnRenderRebindMenu()
        {
            GUILayout.BeginHorizontal(CachedGUIStylesNames.Box);

            DisplayCurrentKey();

            DisplayInputType();

            GUILayout.EndHorizontal();
        }
    }
}