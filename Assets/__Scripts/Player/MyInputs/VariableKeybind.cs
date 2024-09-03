using Inputs;
using Menus;
using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;

namespace Inputs
{
    [Serializable]
    public sealed class VariableKeybind : Keybind
    {
        [SerializeField] private List<InputType> allowedInputTypes;

        private int activationTypeIndex;
        private int activationTypesLength;

        /// <summary>
        /// First provided activation type will be selected as default
        /// </summary>
        /// <param name="relevantKey"></param>
        /// <param name="allowedInputTypes_"></param>
       
        public VariableKeybind(KeyCode relevantKey, List<InputType> allowedInputTypes_, string name_, bool canBeRemapped_, float _holdForSeconds = 0.0f)
        {
            RelevantKey = relevantKey;
            relevantKeyAsStr = relevantKey.ToString();
            allowedInputTypes = allowedInputTypes_;
            inputType = allowedInputTypes[0];
            inputTypeAsStr = inputType.ToString();
            name = name_;
            canBeRemapped = canBeRemapped_;
            holdForSeconds = _holdForSeconds;
        }

        public override void Init()
        {
            base.Init();
            activationTypeIndex = allowedInputTypes.IndexOf(inputType);
            activationTypesLength = allowedInputTypes.Count;
        }

        public void SetActivationType(InputType newActivationType) // there would be issues with index so far
        {
            inputType = newActivationType;
            inputTypeAsStr = inputType.ToString() ;
            SetRelevantOutputSettings();
        }

        public void NextActivationType()
        {
            SetActivationType(allowedInputTypes[++activationTypeIndex % activationTypesLength]);
        }

        public override void DisplayInputType()
        {
            if (GUILayout.Button(inputTypeAsStr, GUILayout.Width(MenuData.RemapInput.InputTypeDisplayWidth)))
            {
                NextActivationType();
            }
        }

        public override bool OnRenderRebindMenu()
        {
            GUILayout.BeginHorizontal(CachedGUIStylesNames.Box);

            var isRemappingAKey = DisplayCurrentKey();

            DisplayInputType();

            GUILayout.EndHorizontal();

            return isRemappingAKey;
        }
    }
}