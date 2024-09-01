using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;

namespace Inputs
{
    [Serializable]
    public sealed class GroupKeybind : Keybind
    {
        private readonly Dictionary<string, Func<bool>> groupEntries;
        public GroupKeybind(KeyCode relevantKey, Dictionary<string, PlayerInputType> actioNameAndInputType, string name_)
        {
            RelevantKey = relevantKey;
            relevantKeyAsStr = relevantKey.ToString();
            name = name_;
            groupEntries = new();
            foreach (KeyValuePair<string, PlayerInputType> kvp in actioNameAndInputType)
            {
                groupEntries[kvp.Key] = GetRelevantOutputSettingsFromParam(kvp.Value);
            }
        }

        public GroupKeybind(KeyCode relevantKey, Dictionary<string, PlayerInputType> actioNameAndInputType, float _holdForSeconds, string name_)
        {
            RelevantKey = relevantKey;
            relevantKeyAsStr = relevantKey.ToString();
            holdForSeconds = _holdForSeconds;
            name = name_;
            groupEntries = new();
            foreach (KeyValuePair<string, PlayerInputType> kvp in actioNameAndInputType)
            {
                groupEntries[kvp.Key] = GetRelevantOutputSettingsFromParam(kvp.Value);
            }
        }

        public static implicit operator bool(GroupKeybind bind)
        {
            throw new Exception("Should not use implicit operator syntax with a GroupKeybind");
        }

        public bool this[string actionNames]
        {
            get
            {
                if (!groupEntries.ContainsKey(actionNames)) { throw new Exception("Should not use imlplicit operator syntax with a group Keybind"); }

                return groupEntries[actionNames]();
            }
        }

        public override void ResetHeldSince()
        {
            heldSince = Time.time;
        }

        public override void OnRenderRebindMenu()
        {
            GUILayout.BeginHorizontal(CachedGUIStylesNames.Box);

            DisplayCurrentKey();

            GUILayout.EndHorizontal();
        }
    }
}