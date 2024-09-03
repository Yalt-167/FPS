using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;

using GameManagement;
using SaveAndLoad;
using Menus;
using Inputs;
using System.Diagnostics;

namespace Inputs
{
    [Serializable]
    public sealed class InputManager : MonoBehaviour, IHaveSomethingToSave, IGameSettingsMenuMember
    {

        [HideInInspector] public InputManagerSaveablePart BindsAndValues;

        #region SaveAndLoad

        public IAmSomethingToSave DataToSave => BindsAndValues;
        public string SaveFilePath { get; } = "keybinds";
        public bool Loaded { get; private set; }


        #endregion

        #region Ease of access

        public MovementInputQuery MovementInputs => BindsAndValues.MovementInputs;
        public CombatInputQuery CombatInputs => BindsAndValues.CombatInputs;
        public GeneralInputQuery GeneralInputs => BindsAndValues.GeneralInputs;
        public float CameraHorizontalSensitivity => BindsAndValues.CameraHorizontalSensitivity;
        public float CameraVerticalSensitivity => BindsAndValues.CameraVerticalSensitivity;

        #endregion

        #region Miscellaneaous

        private RebindMenu currentRebindMenu = RebindMenu.General;
        private static readonly int tabsCount = Enum.GetValues(typeof(RebindMenu)).Length;
        private static readonly string General = nameof(General);
        private static readonly string Combat = nameof(Combat);
        private static readonly string Movement = nameof(Movement);

        private static string MapCurrentRebindMenuToString(RebindMenu rebindMenu)
        {
            return rebindMenu switch
            {
                RebindMenu.General => General,
                RebindMenu.Combat => Combat,
                RebindMenu.Movement => Movement,
                _ => throw new Exception($"This rebind({rebindMenu}) menu does not exist")
            };
        }


        private static Vector2 scrollPosition;

        #endregion

        #region Menu

        private GameSettingsMenu gameSettingsMenu;
        public string MenuName { get; } = "Inputs";
        public int MenuTabsCount { get; } = 3;

        #endregion

        public void Awake()
        {
            StartCoroutine(InitWhenLoaded());
        }

        public IEnumerator InitWhenLoaded()
        {
            yield return new WaitUntil(() => Loaded);

            gameSettingsMenu = GetComponent<GameSettingsMenu>();
            gameSettingsMenu.Subscribe(this);
        }

        private void Update()
        {
            if (Game.Manager.GameStarted && !(MovementInputs.IsRebindingAKey || CombatInputs.IsRebindingAKey || GeneralInputs.IsRebindingAKey))
            {
                if (GeneralInputs.TogglePauseMenu)
                {
                    gameSettingsMenu.ToggleMenu();
                }
            }
        }

        public void OnRenderMenu()
        {
            GUILayout.BeginHorizontal(GUILayout.Width(Screen.width));

            GUILayout.FlexibleSpace();

            GUILayout.BeginVertical(GUILayout.Height(Screen.height));

            GUILayout.BeginVertical(CachedGUIStylesNames.Box);

            #region Rebind menus selection tabs

            GUILayout.BeginHorizontal(CachedGUIStylesNames.Box);

            for (int tabIndex = 0; tabIndex < tabsCount; tabIndex++)
            {
                GUILayout.BeginHorizontal(CachedGUIStylesNames.Box, GUILayout.Width(MenuData.SubMenuTabWidth));

                var relevantRebindMenuEnumMember = (RebindMenu)tabIndex;
                GUI.enabled = currentRebindMenu != relevantRebindMenuEnumMember;

                if (GUILayout.Button(MapCurrentRebindMenuToString(relevantRebindMenuEnumMember))) { currentRebindMenu = relevantRebindMenuEnumMember; }

                GUI.enabled = true;

                GUILayout.EndHorizontal();
            }

            GUILayout.EndHorizontal();

            #endregion

            IInputQuery relevantInputQuery = currentRebindMenu switch
            {
                RebindMenu.Movement => MovementInputs,
                RebindMenu.Combat => CombatInputs,
                RebindMenu.General => GeneralInputs,
                _ => null
            };

            scrollPosition = GUILayout.BeginScrollView(scrollPosition, GUILayout.Height(Screen.height - 100));
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

        public void Load()
        {
            InputManagerSaveablePart? loadedInstance = (InputManagerSaveablePart?)SaveAndLoad.SaveAndLoad.Load(SaveFilePath);

            var success = loadedInstance != null;

            BindsAndValues = (InputManagerSaveablePart) (success ? loadedInstance: new InputManagerSaveablePart().SetDefault());

            if (!success)
            {
                BindsAndValues.MovementInputs.Init();
                BindsAndValues.CombatInputs.Init();
                BindsAndValues.GeneralInputs.Init();
            }

            Loaded = true;
        }
    }
}


public static class idk
{
    public static void test()
    {
        //var tabCount = Enum.GetValues(typeof(RebindMenu)).Length;
        //for (int tabIndex = 0; tabIndex < tabCount; tabIndex++)
        //{
        //    GUILayout.BeginHorizontal(CachedGUIStylesNames.Box, GUILayout.Width(MenuData.SubMenuTabWidth));

        //    GUI.enabled = currentRebindMenu != (RebindMenu)tabIndex;

        //    if (GUILayout.Button(((RebindMenu)tabIndex).ToString())) { currentRebindMenu = (RebindMenu)tabIndex; }

        //    GUI.enabled = true;

        //    GUILayout.EndHorizontal();
        //}



        //#region General button

        //GUILayout.BeginHorizontal(CachedGUIStylesNames.Box, GUILayout.Width(MenuData.SubMenuTabWidth));

        //GUI.enabled = currentRebindMenu != RebindMenu.General;

        //if (GUILayout.Button(General)) { currentRebindMenu = RebindMenu.General; }

        //GUI.enabled = true;

        //GUILayout.EndHorizontal();

        //#endregion // do that with a loop XD

        //#region Movement button

        //GUILayout.BeginHorizontal(CachedGUIStylesNames.Box, GUILayout.Width(MenuData.SubMenuTabWidth));

        //GUI.enabled = currentRebindMenu != RebindMenu.Movement;

        //if (GUILayout.Button(Movement)) { currentRebindMenu = RebindMenu.Movement; }

        //GUI.enabled = true;

        //GUILayout.EndHorizontal();

        //#endregion

        //#region Combat button

        //GUILayout.BeginHorizontal(CachedGUIStylesNames.Box, GUILayout.Width(MenuData.SubMenuTabWidth));

        //GUI.enabled = currentRebindMenu != RebindMenu.Combat;

        //if (GUILayout.Button(Combat)) { currentRebindMenu = RebindMenu.Combat; }

        //GUI.enabled = true;

        //GUILayout.EndHorizontal();

        //#endregion
    }
}