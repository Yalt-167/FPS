using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;

public class SceneController : MonoBehaviour
{
    public string LevelName;

    void OnTriggerEnter(Collider col)
    {
        if (col.CompareTag("Player"))
        {
            SceneManager.LoadScene(LevelName);
        }
    }
}
