using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;

namespace SaveAndLoad
{
    public interface IHaveSomethingToSave
    {
        public void Save();

        /// <summary>
        /// Returns wether a save file was found
        /// </summary>
        /// <returns></returns>
        public bool Load();

        public IAmSomethingToSave DataToSave { get; }

        public string SaveFilePath { get; }
    }
}