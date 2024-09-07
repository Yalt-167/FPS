using Menus;
using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;

namespace SaveAndLoad
{
    public interface IHaveSomethingToSave
    {
        public SaveDataInfo[] SaveDataInfos { get; }
        public int AmountOfThingsToSave { get; }

        public bool Loaded { get; }

        public void Save();
        public void Load();
        public IEnumerator InitWhenLoaded();

#if !false
        public void Awake()
        {
            Utility.CoroutineStarter.Instance.StartCoroutine(InitWhenLoaded());
        }

        public SaveDataInfo[] SaveDataInfos { get; } = new SaveDataInfo[]
        {
            new SaveDataInfo(Data1, typeof(Data1), "Data1Path"),
            new SaveDataInfo(Data2, typeof(Data2), "Data2Path"),
            new SaveDataInfo(..., typeof(...), "..."),
        };

        public bool Loaded { get; private set; }
        public int AmountOfThingsToSave { get; }
        public IEnumerator InitWhenLoaded()
        {
            yield return new WaitUntil(() => Loaded);

            // your Init logic
        }

        public void Save()
        {
            for (int dataIndex = 0; dataIndex < AmountOfThingsToSave; dataIndex++)
            {
                SaveAndLoad.SaveAndLoad.Save(DataToSave[dataIndex].DataToSave, DataToSave[dataIndex].SaveFilePath);
            }
        }

        public void Load()
        {
            for (int dataIndex = 0; dataIndex < AmountOfThingsToSave; dataIndex++)
            {
                DataToSave[dataIndex].Datatype ? loadedInstance = (DataToSave[dataIndex].Datatype ?)SaveAndLoad.SaveAndLoad.Load(DataToSave[dataIndex].SaveFilePath);
                
            }

            var success = loadedInstance != null;

            BindsAndValues = (DataToSave[dataIndex].Datatype)(success ? loadedInstance : new DataToSave[dataIndex].Datatype().SetDefault());

            if (!success)
            {
                BindsAndValues.MovementInputs.Init();
                BindsAndValues.CombatInputs.Init();
                BindsAndValues.GeneralInputs.Init();
            }

            Loaded = true;
        }

#endif
    }
}