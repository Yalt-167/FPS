using GameManagement;
using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.UI;

namespace Inputs
{
    public sealed class InputManager : MonoBehaviour
    {
        public MovementInputQuery MovementInputs;
        public CombatInputQuery CombatInputs;
        public GeneralInputQuery GeneralInputs;
        public float cameraHorizontalSenitivity = 3f;
        public float cameraVerticalSensitivity = 3f;
        private bool doRenderMenu;
        private CurrentRebindMenu currentRebindMenu = CurrentRebindMenu.General;

        private void Awake()
        {
            MovementInputs.Init();
            CombatInputs.Init();
            GeneralInputs.Init();
        }

        private void Update()
        {
            doRenderMenu = GeneralInputs.TogglePauseMenu ? !doRenderMenu : doRenderMenu;

            if (doRenderMenu || !Game.Manager.GameStarted) { PlayerFrame.LocalPlayer.SetMenuInputMode(); }
            else { PlayerFrame.LocalPlayer.SetGameplayInputMode(); }
        }

        private void OnGUI()
        {
            if (!doRenderMenu) { return; }

            GUILayout.BeginHorizontal(GUILayout.Width(Screen.width));

            GUILayout.FlexibleSpace();

            GUILayout.BeginVertical(GUILayout.Height(Screen.height));

            GUILayout.BeginVertical(CachedGUIStylesNames.Box);

            GUILayout.BeginHorizontal(CachedGUIStylesNames.Box);

            GUILayout.BeginHorizontal(CachedGUIStylesNames.Box);

            GUI.color = currentRebindMenu == CurrentRebindMenu.General ? Color.grey : Color.white;

            if (GUILayout.Button("General"))
            {
                currentRebindMenu = CurrentRebindMenu.General;
            }

            GUI.color = Color.white;

            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal(CachedGUIStylesNames.Box);

            GUI.color = currentRebindMenu == CurrentRebindMenu.Movement ? Color.grey : Color.white;

            if (GUILayout.Button("Movement"))
            {
                currentRebindMenu = CurrentRebindMenu.Movement;
            }

            GUI.color = Color.white;

            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal(CachedGUIStylesNames.Box);

            GUI.color = currentRebindMenu == CurrentRebindMenu.Combat ? Color.grey : Color.white;

            if (GUILayout.Button("Combat"))
            {
                currentRebindMenu = CurrentRebindMenu.Combat;
            }

            GUI.color = Color.white;

            GUILayout.EndHorizontal();

            GUILayout.EndHorizontal();
            
            IInputQuery relevantInputQuery = currentRebindMenu switch
            {
                CurrentRebindMenu.Movement => MovementInputs,
                CurrentRebindMenu.Combat => CombatInputs,
                CurrentRebindMenu.General => GeneralInputs,
                _ => null
            };

            relevantInputQuery?.OnRenderRebindMenu();

            GUILayout.EndVertical();

            GUILayout.EndVertical();

            GUILayout.FlexibleSpace();

            GUILayout.EndHorizontal();
        }
    }
}