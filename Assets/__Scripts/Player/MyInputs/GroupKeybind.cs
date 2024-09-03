using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;

namespace Inputs
{
    [Serializable]
    public sealed class GroupKeybind : Keybind
    {
        private readonly Dictionary<InputType, Func<bool>> groupEntries;
  
        public GroupKeybind(KeyCode relevantKey, InputType[] inputTypes, string name_, bool canBeRemapped_, float _holdForSeconds = 0.0f)
        {
            RelevantKey = relevantKey;
            relevantKeyAsStr = relevantKey.ToString();
            name = name_;
            groupEntries = new();
            foreach (InputType inputType in inputTypes)
            {
                groupEntries[inputType] = GetRelevantOutputSettingsFromParam(inputType);
            }
            canBeRemapped = canBeRemapped_;
            holdForSeconds = _holdForSeconds;
        }

        public static implicit operator bool(GroupKeybind bind)
        {
            throw new Exception("Should not use implicit operator syntax with a GroupKeybind");
        }

        public bool this[InputType actionName]
        {
            get
            {
                if (!groupEntries.ContainsKey(actionName)) { throw new Exception("This group keybind does not support this input type"); }

                return groupEntries[actionName]();
            }
        }

        public override void ResetHeldSince()
        {
            heldSince = Time.time;
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