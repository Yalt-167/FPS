using GameManagement;
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

        private int currentMenuIndex;

        public int Subscribe(IGameSettingsMenuMember menu)
        {
            menus.Add(menu);
            return menus.Count - 1;
        }

        public void ToggleMenu()
        {
            doRenderMenu = !doRenderMenu;

            if (doRenderMenu || !Game.Manager.GameStarted) { PlayerFrame.LocalPlayer.SetMenuInputMode(); }
            else { PlayerFrame.LocalPlayer.SetGameplayInputMode(); }
        }



        private void OnGUI()
        {
            if (!doRenderMenu) { return; }

            menus[currentMenuIndex].OnRenderMenu();

        }
    }
}