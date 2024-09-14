using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;

using GameManagement;

namespace Menus
{
    public sealed class GameSettingsMenu : MonoBehaviour
    {
        private readonly List<IGameSettingsMenuMember> menus = new();
        private bool doRenderMenu;
        public bool DoRenderMenu => doRenderMenu;
        private int maxMenuColumns;

        private int currentMenuIndex;

        public void Subscribe(IGameSettingsMenuMember menu)
        {
            menus.Add(menu);

            maxMenuColumns = maxMenuColumns < menu.MenuColumnsCount ? menu.MenuColumnsCount : maxMenuColumns;
        }

        public void ToggleMenu()
        {
            doRenderMenu = !doRenderMenu;

            if (!Game.Started || doRenderMenu)
            {
                PlayerFrame.LocalPlayer.SetMenuInputMode();
            }
            else
            {
                PlayerFrame.LocalPlayer.SetGameplayInputMode();
            }
        }

        private void OnGUI()
        {
            if (!doRenderMenu) { return; }

            GUILayout.BeginVertical();
            GUILayout.Space(15);

            #region Top Menu Tabs

            GUILayout.BeginHorizontal(GUILayout.Width(Screen.width));

            GUILayout.FlexibleSpace();

            GUILayout.BeginHorizontal(CachedGUIStylesNames.Box, GUILayout.Width(MenuData.GameSettingsMenuWidth));

            for (int i = 0; i < menus.Count; i++)
            {
                GUILayout.BeginHorizontal(CachedGUIStylesNames.Box);

                GUI.enabled = currentMenuIndex != i;

                if (GUILayout.Button(menus[i].MenuName, GUILayout.Width(MenuData.GameSettingsMenuWidth / menus.Count))) { currentMenuIndex = i; }

                GUI.enabled = true;

                GUILayout.EndHorizontal();
            }

            GUILayout.EndHorizontal();

            GUILayout.FlexibleSpace();

            GUILayout.EndHorizontal();

            #endregion

            menus[currentMenuIndex].OnRenderMenu();

            GUILayout.Space(15);
            GUILayout.EndVertical();

        }
    }
}