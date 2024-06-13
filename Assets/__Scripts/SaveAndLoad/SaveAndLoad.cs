using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

public static class SaveAndLoad
{
    public static void Save(object data, string name)
    {
        BinaryFormatter binaryFormater = new();
        string path = $"{Application.persistentDataPath}/{name}.data";

        using (FileStream file = new(path, FileMode.Create))
        {
            binaryFormater.Serialize(file, data);
        }
    }


    public static object Load(string name)
    {
        string path = $"{Application.persistentDataPath}/{name}.data";

        if (!File.Exists(path))
        {
            Debug.Log($"No save file found for path: {path}");
            return null;
        }
        
        BinaryFormatter binaryFormater = new();

        object data;
        using (FileStream file = new(path, FileMode.Open))
        {
            data = binaryFormater.Deserialize(file);
        }

        return data;

    }
}
