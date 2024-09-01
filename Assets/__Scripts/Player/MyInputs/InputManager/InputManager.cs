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
        private RebindMenu currentRebindMenu = RebindMenu.General;
        private static readonly string General = nameof(General);
        private static readonly string Combat = nameof(Combat);
        private static readonly string Movement = nameof(Movement);
        private static Vector2 scrollPosition;

        private void Awake()
        {
            MovementInputs.Init();
            MovementInputs.Init();
            MovementInputs.Init();
            MovementInputs.Init();
            MovementInputs.Init();
            CombatInputs.Init();
            GeneralInputs.Init();
        }

        private void Update()
        {
            if (!(MovementInputs.IsRebindingAKey || CombatInputs.IsRebindingAKey || GeneralInputs.IsRebindingAKey))
            {
                doRenderMenu = GeneralInputs.TogglePauseMenu ? !doRenderMenu : doRenderMenu;
            }

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

            #region Rebind menus selection tabs

            GUILayout.BeginHorizontal(CachedGUIStylesNames.Box);

            #region General button

            GUILayout.BeginHorizontal(CachedGUIStylesNames.Box, GUILayout.Width(200));

            GUI.enabled = currentRebindMenu != RebindMenu.General;

            if (GUILayout.Button(General)) { currentRebindMenu = RebindMenu.General; }

            GUI.enabled = true;

            GUILayout.EndHorizontal();

            #endregion

            #region Movement button

            GUILayout.BeginHorizontal(CachedGUIStylesNames.Box, GUILayout.Width(200), GUILayout.Width(200));

            GUI.enabled = currentRebindMenu != RebindMenu.Movement;

            if (GUILayout.Button(Movement)) { currentRebindMenu = RebindMenu.Movement; }

            GUI.enabled = true;

            GUILayout.EndHorizontal();

            #endregion

            #region Combat button

            GUILayout.BeginHorizontal(CachedGUIStylesNames.Box, GUILayout.Width(200));

            GUI.enabled = currentRebindMenu != RebindMenu.Combat;

            if (GUILayout.Button(Combat)) { currentRebindMenu = RebindMenu.Combat; }

            GUI.enabled = true;

            GUILayout.EndHorizontal();

            #endregion

            GUILayout.EndHorizontal();

            #endregion

            IInputQuery relevantInputQuery = currentRebindMenu switch
            {
                RebindMenu.Movement => MovementInputs,
                RebindMenu.Combat => CombatInputs,
                RebindMenu.General => GeneralInputs,
                _ => null
            };

            scrollPosition = GUILayout.BeginScrollView(scrollPosition);
            relevantInputQuery?.OnRenderRebindMenu();
            GUILayout.EndScrollView();

            GUILayout.EndVertical();

            GUILayout.EndVertical();

            GUILayout.FlexibleSpace();

            GUILayout.EndHorizontal();
        }
    }
}