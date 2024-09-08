using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;

using Menus;
using SaveAndLoad;

namespace Controller
{
    public sealed class MovementPreferences : MonoBehaviour, IPreferenceMenuMember, IHaveSomethingToSave
    {
        private PreferenceMenu preferenceMenu;

        private MovementPreferenceSave MovementPreferencesValues
        {
            get
            {
                return (MovementPreferenceSave)DataToSave[0];
            }
            set
            {
                DataToSave[0] = value;
            }
        }

        public IAmSomethingToSave[] DataToSave { get; private set; }

        public string[] SaveFilePaths { get; private set; }

        public int AmountOfThingsToSave { get; } = 1;

        public bool Loaded { get; private set; }

        private void Awake()
        {
            preferenceMenu = GetComponent<PreferenceMenu>();
            preferenceMenu.Subscribe(this);

            DataToSave = new IAmSomethingToSave[] { new MovementPreferenceSave() };
            SaveFilePaths = new string[] { "MovementPreferences" };
        }


        public void OnRenderMenu()
        {
            GUILayout.BeginHorizontal(CachedGUIStylesNames.Box);
            GUILayout.Label("I was drawn", GUILayout.Width(MenuData.GameSettingsMenuWidth));
            GUILayout.EndHorizontal();
        }

        public void Save()
        {
            for (int dataIndex = 0; dataIndex < AmountOfThingsToSave; dataIndex++)
            {
                SaveAndLoad.SaveAndLoad.Save(DataToSave[dataIndex], SaveFilePaths[dataIndex]);
            }
        }

        public void Load()
        {
            MovementPreferenceSave? loadedInstance = (MovementPreferenceSave?)SaveAndLoad.SaveAndLoad.Load(SaveFilePaths[0]);

            MovementPreferencesValues = (MovementPreferenceSave)(loadedInstance != null ? loadedInstance : new MovementPreferenceSave().SetDefault().Init());

            Loaded = true;
        }

        public IEnumerator InitWhenLoaded()
        {
            yield return new WaitUntil(() => Loaded);

            
        }
    }
}