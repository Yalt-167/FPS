using SaveAndLoad;
using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;

namespace Menus
{
    public sealed class PreferenceMenu : MonoBehaviour, IGameSettingsMenuMember, IHaveSomethingToSave
    {

        #region Save and Load

        public bool Loaded { get; private set; } 

        public IAmSomethingToSave[] DataToSave { get; private set; }
        public string[] SaveFilePaths { get; private set; }
        public int AmountOfThingsToSave { get; } = 1;
        private Vector2 scrollPosition = Vector2.zero;

        public void Save()
        {
            
        }

        public void Load()
        {
            

            Loaded = true;
        }

        public IEnumerator InitWhenLoaded()
        {
            yield return new WaitUntil(() => Loaded);

            Init();
        }

        #endregion

        #region Game Settings Menu related

        public string MenuName { get; } = "Preferences";

        public int MenuColumnsCount { get; } = 1;

        private GameSettingsMenu gameSettingsMenu;

        #endregion

        private readonly List<IPreferenceMenuMember> preferenceMenuEntries = new();

        public void Awake()
        {
            DataToSave = new IAmSomethingToSave[] { };
            SaveFilePaths = new string[] { };

            StartCoroutine(InitWhenLoaded());
        }


        private void Init()
        {
            gameSettingsMenu = GetComponent<GameSettingsMenu>();
            gameSettingsMenu.Subscribe(this);
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