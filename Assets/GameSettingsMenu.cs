using GameManagement;
using Inputs;
using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;

namespace Menus
{
    public sealed class GameSettingsMenu : MonoBehaviour
    {
        private readonly List<IGameSettingsMenuMember> menus = new();
        private bool doRenderMenu;

        private int maxMenuTabs;

        private int currentMenuIndex;

        public void Subscribe(IGameSettingsMenuMember menu)
        {
            menus.Add(menu);

            maxMenuTabs = maxMenuTabs < menu.MenuTabsCount ? menu.MenuTabsCount : maxMenuTabs;
        }

        public void ToggleMenu()
        {
            doRenderMenu = !doRenderMenu;

            if (doRenderMenu || !Game.Manager.GameStarted)
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

            GUILayout.BeginHorizontal(CachedGUIStylesNames.Box, GUILayout.Width(maxMenuTabs * MenuData.SubMenuTabWidth));

            for (int i = 0; i < menus.Count; i++)
            {
                GUILayout.BeginHorizontal(CachedGUIStylesNames.Box, GUILayout.ExpandWidth(true));

                GUI.enabled = currentMenuIndex != i;

                if (GUILayout.Button(menus[i].MenuName)) { currentMenuIndex = i; }

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