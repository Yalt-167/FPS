using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;

using GameManagement;
using SaveAndLoad;

namespace Inputs
{
    [Serializable]
    public sealed class InputManager : MonoBehaviour, IHaveSomethingToSave
    {
        public InputManagerSaveablePart BindsAndValues;
        public IAmSomethingToSave DataToSave => BindsAndValues;
        public string SaveFilePath { get; } = "keybinds";

        private bool doRenderMenu;
        private RebindMenu currentRebindMenu = RebindMenu.General;
        private static readonly string General = nameof(General);
        private static readonly string Combat = nameof(Combat);
        private static readonly string Movement = nameof(Movement);
        private static Vector2 scrollPosition;



        public MovementInputQuery MovementInputs => BindsAndValues.MovementInputs ;
        public CombatInputQuery CombatInputs => BindsAndValues.CombatInputs;
        public GeneralInputQuery GeneralInputs => BindsAndValues.GeneralInputs;
        public float CameraHorizontalSensitivity => BindsAndValues.CameraHorizontalSensitivity;
        public float CameraVerticalSensitivity => BindsAndValues.CameraVerticalSensitivity;


        public void Awake()
        {
            _ = Load();

            MovementInputs.Init();
            CombatInputs.Init();
            GeneralInputs.Init();
        }

        private void Update()
        {
            if (Game.Manager.GameStarted && !(MovementInputs.IsRebindingAKey || CombatInputs.IsRebindingAKey || GeneralInputs.IsRebindingAKey))
            {
                doRenderMenu = GeneralInputs.TogglePauseMenu ? !doRenderMenu : doRenderMenu;
            }

            if (doRenderMenu || !Game.Manager.GameStarted) { PlayerFrame.LocalPlayer.SetMenuInputMode(); }
            else { PlayerFrame.LocalPlayer.SetGameplayInputMode(); }

            if (Input.GetKeyDown(KeyCode.M))
            {
                Save();
            }
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

        public void Save()
        {
            SaveAndLoad.SaveAndLoad.Save(DataToSave, SaveFilePath);
        }

        public bool Load()
        {
            InputManagerSaveablePart? loadedInstance = (InputManagerSaveablePart?)SaveAndLoad.SaveAndLoad.Load(SaveFilePath);

            var success = loadedInstance != null;

            BindsAndValues = (InputManagerSaveablePart) (success ? loadedInstance: new InputManagerSaveablePart().SetDefault());

            return success;
        }

        //private void OnDisable()
        //{
        //    Save();
        //}
    }
}