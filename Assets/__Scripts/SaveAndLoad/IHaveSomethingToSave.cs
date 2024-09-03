using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;

namespace SaveAndLoad
{
    public interface IHaveSomethingToSave
    {
        public void Save();

        public void Load();
        public bool Loaded { get; }

        public IEnumerator InitWhenLoaded();

        public IAmSomethingToSave DataToSave { get; }

        public string SaveFilePath { get; }
    }
}