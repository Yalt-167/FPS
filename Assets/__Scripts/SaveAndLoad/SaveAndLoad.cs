using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine.SceneManagement;

public static class SaveAndLoad
{
    public static void Save(float totalTime)
    {
        BinaryFormatter binaryFormater = new();
        string path = Application.persistentDataPath + $"/{SceneManager.GetActiveScene().name}.coolStuff";

        FileStream file = new(path, FileMode.Create);

        binaryFormater.Serialize(file, totalTime);
        file.Close();
    }


    public static float Load()
    {
        string path = Application.persistentDataPath + $"/{SceneManager.GetActiveScene().name}.coolStuff";

        if (File.Exists(path))
        {
            BinaryFormatter binaryFormater = new();
            FileStream file = new(path, FileMode.Open);

            float totalTime = (float)binaryFormater.Deserialize(file);
            file.Close();
            return totalTime;
        }
        else
        {
            Debug.Log("No save file found");
            return -1f;
        }

    }
}
