using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;

namespace SaveAndLoad
{
    public struct SaveDataInfo
    {
        public IAmSomethingToSave DataToSave;
        public Type DataType;
        public string SaveFilePath;

        public SaveDataInfo(IAmSomethingToSave data, Type dataType, string saveFilePath)
        {
            DataToSave = data;
            DataType = dataType;
            SaveFilePath = saveFilePath;
        }
    }
}