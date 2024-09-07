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

        public IAmSomethingToSave DataToSave => throw new NotImplementedException();

        public string SaveFilePath { get; } = "Preferences";

        public void Save()
        {
            throw new NotImplementedException();
        }

        public void Load()
        {
            throw new NotImplementedException();
        }

        public IEnumerator InitWhenLoaded()
        {
            yield return new WaitUntil(() => Loaded);

            Init();
        }

        #endregion


        public string MenuName { get; } = "Preferences";

        public int MenuTabsCount { get; } = 1;

        private readonly List<IPreferenceMenuMember> preferenceMenuEntries;

        public void Awake()
        {
            StartCoroutine(InitWhenLoaded());
        }


        private void Init()
        {
            
        }

        public void OnRenderMenu()
        {
            GUILayout.BeginHorizontal(GUILayout.Width(Screen.width));

            GUILayout.FlexibleSpace();

            GUILayout.BeginVertical(GUILayout.Height(Screen.height));

            GUILayout.BeginVertical(CachedGUIStylesNames.Box);






            GUILayout.EndVertical();

            GUILayout.EndVertical();

            GUILayout.FlexibleSpace();

            GUILayout.EndHorizontal();
        }
    }
}