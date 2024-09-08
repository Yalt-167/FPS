using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;

using MyCollections;
namespace SaveAndLoad
{
    public interface IHaveSomethingToSave
    {
        public IAmSomethingToSave[] DataToSave { get; } 
        public string[] SaveFilePaths { get; }
        public int AmountOfThingsToSave { get; }

        public bool Loaded { get; }

        public void Save();
        public void Load();
        public IEnumerator InitWhenLoaded();

#if false

        public IAmSomethingToSave[] DataToSave { get; private set;}
        public string[] SaveFilePaths { get; private set; }
        public bool Loaded { get; private set; }


        public void Awake()
        {
            DataToSave = new IAmSomethingToSave[] { Data1, Data2, ...};
            SaveFilePaths = new string[] { "Data1Path", "Data2Path", ... };

            Utility.CoroutineStarter.Instance. // if not in monobehaviour
            StartCoroutine(InitWhenLoaded());
        }

        public IEnumerator InitWhenLoaded()
        {
            yield return new WaitUntil(() => Loaded);

            // your Init logic
        }


        

        public void Save()
        {
            for (int dataIndex = 0; dataIndex < AmountOfThingsToSave; dataIndex++)
            {
                SaveAndLoad.SaveAndLoad.Save(DataToSave[dataIndex], SaveFilePath[dataIndex]);
            }
        }

        public void Load()
        {
        // repeat those two steps for each iteration 
            YourDataStruct1? loadedInstance = (YourDataStruct1?)SaveAndLoad.SaveAndLoad.Load(SaveFilePath);

            yourActualVariable = (YourDataStruct1)(loadedInstance != null ? loadedInstance : new YourDataStruct1().SetDefault());

            Loaded = true;
        }
#endif
    }
}