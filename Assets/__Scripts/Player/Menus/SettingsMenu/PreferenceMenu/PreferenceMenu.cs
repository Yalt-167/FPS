using SaveAndLoad;
using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;

namespace Menus
{
    public sealed class PreferenceMenu : MonoBehaviour, IGameSettingsMenuMember
    {

        private Vector2 scrollPosition = Vector2.zero;
        #region Game Settings Menu related

        public string MenuName { get; } = "Preferences";

        public int MenuColumnsCount { get; } = 1;

        public GameSettingsMenu GameSettingsMenu { get; private set; }

        #endregion

        private readonly List<IPreferenceMenuMember> preferenceMenuEntries = new();

        public void Awake()
        {
            GameSettingsMenu = GetComponent<GameSettingsMenu>();
            GameSettingsMenu.Subscribe(this);
        }


        public void Subscribe(IPreferenceMenuMember member)
        {
            preferenceMenuEntries.Add(member);
        }

        public void OnRenderMenu()
        {
            GUILayout.BeginHorizontal(GUILayout.Width(Screen.width));

            GUILayout.FlexibleSpace();

            GUILayout.BeginVertical(GUILayout.Height(Screen.height));

            GUILayout.BeginVertical(CachedGUIStylesNames.Box);

            scrollPosition = GUILayout.BeginScrollView(scrollPosition, GUILayout.Height(MenuData.GameSettingsMenuScrollableRegionHeight));
            foreach (var entry in preferenceMenuEntries)
            {
                entry.OnRenderMenu();
            }
            GUILayout.EndScrollView();

            GUILayout.EndVertical();

            GUILayout.EndVertical();

            GUILayout.FlexibleSpace();

            GUILayout.EndHorizontal();
        }
    }
}