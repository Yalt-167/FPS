using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

public static class SaveAndLoad
{
    public static void Save(object data, string name)
    {
        string path = $"{Application.persistentDataPath}/{name}.data";

        using (FileStream file = new(path, FileMode.Create))
        {
            new BinaryFormatter().Serialize(file, data);
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

        object data;
        using (FileStream file = new(path, FileMode.Open))
        {
            data = new BinaryFormatter().Deserialize(file);
        }

        return data;

    }
}
