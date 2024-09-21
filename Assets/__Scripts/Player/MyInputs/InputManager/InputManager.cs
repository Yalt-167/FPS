using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;

using GameManagement;
using SaveAndLoad;
using Menus;
using MyCollections;

namespace Inputs
{
    [Serializable]
    public sealed class InputManager : MonoBehaviour, IHaveSomethingToSave, IGameSettingsMenuMember
    {
        private KeybindsSave BindsAndValues
        {
            get
            {
                return (KeybindsSave)DataToSave[0];
            }
            set
            {
                DataToSave[0] = value;
            }
        }
        
        #region SaveAndLoad

        public IAmSomethingToSave[] DataToSave { get; private set; }
        public string[] SaveFilePaths { get; private set; }

        public int AmountOfThingsToSave { get; } = 1;
        public bool Loaded { get; private set; }

        public IEnumerator InitWhenLoaded()
        {
            yield return new WaitUntil(() => Loaded);
        }

        public void Save()
        {
            SaveAndLoad.SaveAndLoad.Save(DataToSave[0], SaveFilePaths[0]);
        }

        public void Load()
        {
            KeybindsSave? loadedInstance = (KeybindsSave?)SaveAndLoad.SaveAndLoad.Load(SaveFilePaths[0]);

            BindsAndValues = (KeybindsSave)(loadedInstance != null ? loadedInstance : new KeybindsSave().SetDefault().Init());

            Loaded = true;
        }

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

        public GameSettingsMenu GameSettingsMenu {  get; private set; }
        public string MenuName { get; } = "Inputs";
        public int MenuColumnsCount { get; } = 3;

        #endregion

        private bool showScoreboardActive;

        public void Awake()
        {
            DataToSave = new IAmSomethingToSave[1] { new KeybindsSave() };
            SaveFilePaths = new string[1] { "Keybinds" };

            GameSettingsMenu = GetComponent<GameSettingsMenu>();
            GameSettingsMenu.Subscribe(this);

            StartCoroutine(InitWhenLoaded());
        }

        private void Update()
        {
            if (!Game.Started) { return; }

            showScoreboardActive = GeneralInputs.ShowScoreboard;

            if (MovementInputs.IsRebindingAKey || CombatInputs.IsRebindingAKey || GeneralInputs.IsRebindingAKey) { return; }

            if (GeneralInputs.TogglePauseMenu)
            {
                GameSettingsMenu.ToggleMenu();
            }

            FoldableScoreboard.Instance.SetDoRender(!GameSettingsMenu.DoRenderMenu && showScoreboardActive);
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
                GUILayout.BeginHorizontal(CachedGUIStylesNames.Box, GUILayout.Width(200));

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

            InputQuery relevantInputQuery = currentRebindMenu switch
            {
                RebindMenus.Movement => MovementInputs,
                RebindMenus.Combat => CombatInputs,
                RebindMenus.General => GeneralInputs,
                _ => null
            };

            scrollPosition = GUILayout.BeginScrollView(scrollPosition, GUILayout.Height(MenuData.GameSettingsMenuScrollableRegionHeight));
            relevantInputQuery?.OnRenderRebindMenu();
            GUILayout.EndScrollView();

            GUILayout.EndVertical();

            GUILayout.EndVertical();

            GUILayout.FlexibleSpace();

            GUILayout.EndHorizontal();
        }
    }
}