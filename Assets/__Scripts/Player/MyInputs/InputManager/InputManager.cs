using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;

using GameManagement;
using SaveAndLoad;
using Menus;

namespace Inputs
{
    [Serializable]
    public sealed class InputManager : MonoBehaviour, IHaveSomethingToSave, IGameSettingsMenuMember
    {
        [HideInInspector] public InputManagerSaveablePart BindsAndValues;

        #region SaveAndLoad

        public IAmSomethingToSave DataToSave => BindsAndValues;
        public string SaveFilePath { get; } = "Keybinds";
        public bool Loaded { get; private set; }

        #endregion

        #region Ease of access

        public MovementInputQuery MovementInputs => BindsAndValues.MovementInputs;
        public CombatInputQuery CombatInputs => BindsAndValues.CombatInputs;
        public GeneralInputQuery GeneralInputs => BindsAndValues.GeneralInputs;
        public float CameraHorizontalSensitivity => BindsAndValues.CameraHorizontalSensitivity;
        public float CameraVerticalSensitivity => BindsAndValues.CameraVerticalSensitivity;

        #endregion

        #region Rebind Menu

        private RebindMenus currentRebindMenu = RebindMenus.General;
        private static readonly int tabsCount = Enum.GetValues(typeof(RebindMenus)).Length;
        private static readonly string General = nameof(General);
        private static readonly string Combat = nameof(Combat);
        private static readonly string Movement = nameof(Movement);

        private static string MapCurrentRebindMenuToString(RebindMenus rebindMenu)
        {
            return rebindMenu switch
            {
                RebindMenus.General => General,
                RebindMenus.Combat => Combat,
                RebindMenus.Movement => Movement,
                _ => throw new Exception($"This rebind menu ({rebindMenu}) does not exist")
            };
        }


        private static Vector2 scrollPosition;

        #endregion

        #region Menu

        private GameSettingsMenu gameSettingsMenu;
        public string MenuName { get; } = "Inputs";
        public int MenuTabsCount { get; } = 3;

        #endregion

        private bool showScoreboardActive;

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
            if (!Game.Manager.GameStarted) { return; }

            showScoreboardActive = GeneralInputs.ShowScoreboard;

            if (MovementInputs.IsRebindingAKey || CombatInputs.IsRebindingAKey || GeneralInputs.IsRebindingAKey) { return; }

            if (GeneralInputs.TogglePauseMenu)
            {
                gameSettingsMenu.ToggleMenu();
            }

            Scoreboard.Instance.SetDoRender(!gameSettingsMenu.DoRenderMenu && showScoreboardActive);
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

                var relevantRebindMenuEnumMember = (RebindMenus)tabIndex;
                GUI.enabled = currentRebindMenu != relevantRebindMenuEnumMember;

                if (GUILayout.Button(MapCurrentRebindMenuToString(relevantRebindMenuEnumMember)))
                {
                    currentRebindMenu = relevantRebindMenuEnumMember;
                }

                GUI.enabled = true;

                GUILayout.EndHorizontal();
            }

            GUILayout.EndHorizontal();

            #endregion

            IInputQuery relevantInputQuery = currentRebindMenu switch
            {
                RebindMenus.Movement => MovementInputs,
                RebindMenus.Combat => CombatInputs,
                RebindMenus.General => GeneralInputs,
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