using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using UnityEngine;

namespace Inputs
{
    [Serializable]
    public sealed class GroupKeybind : Keybind
    {
        private Dictionary<string, Func<bool>> groupEntries;
        //public GroupKeybind(KeyCode relevantKey, PlayerInputType[] activationTypes, string[] actionNames, string name_)
        public GroupKeybind(KeyCode relevantKey, Dictionary<string, PlayerInputType> actioNameAndInputType, string name_)
        {
            RelevantKey = relevantKey;
            //howToActivate = activationTypes;
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
            //howToActivate = activationTypes;
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
            throw new Exception("Should not use implicit operator syntax with a group Keybind");
        }

        public bool this[string actionNames]
        {
            get
            {
                if (!groupEntries.ContainsKey(actionNames)) { throw new Exception("Should not use imlplicit operator syntax with a group Keybind"); }

                return groupEntries[actionNames]();
            }
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