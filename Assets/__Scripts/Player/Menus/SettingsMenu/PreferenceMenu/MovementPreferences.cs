using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;

using Menus;

namespace Controller
{
    public sealed class MovementPreferences : MonoBehaviour, IPreferenceMenuMember
    {
        private PreferenceMenu preferenceMenu;

        private void Awake()
        {
            preferenceMenu = GetComponent<PreferenceMenu>();
            preferenceMenu.Subscribe(this);
        }


        public void OnRenderMenu()
        {
            GUILayout.BeginHorizontal(CachedGUIStylesNames.Box);
            GUILayout.Label("I was drawn", GUILayout.Width(MenuData.GameSettingsMenuWidth));
            GUILayout.EndHorizontal();
        }
    }
}