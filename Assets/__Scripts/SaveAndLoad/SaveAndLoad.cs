using UnityEngine;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

namespace SaveAndLoad
{
    public static class SaveAndLoad
    {
        public static void Save(object data, string name)
        {
            string path = $"{Application.persistentDataPath}/{name}.data";
            Debug.Log($"Saving to path: {path}");

            using (FileStream file = new(path, FileMode.Create))
            {
                new BinaryFormatter().Serialize(file, data);
            }
        }


        public static object Load(string name)
        {
            string path = $"{Application.persistentDataPath}/{name}.data";
            Debug.Log($"Trying to load from path: {path}");

            if (!File.Exists(path))
            {
                Debug.Log($"No save file found for path: {path}");
                return null;
            }

            Debug.Log($"A save file was found for path: {path}");
            object data;
            using (FileStream file = new(path, FileMode.Open))
            {
                data = new BinaryFormatter().Deserialize(file);
            }
            Debug.Log("Save file was read successfully");
            return data;

        }
    }
} // C:/Users/antoi/AppData/LocalLow/DefaultCompany/ProjectOlympus/{name}.data