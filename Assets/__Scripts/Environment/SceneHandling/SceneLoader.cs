using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.SceneManagement;

namespace SceneHandling
{
    public sealed class SceneLoader : MonoBehaviour
    {
        public static SceneLoader Instance;

        private readonly List<string> loadedScenes = new();

        private void Awake()
        {
            Instance = this;
        }

        public  void LoadSceneAsync(string scene, bool additive)
        {
            if (SceneMissing(scene)) { return; }

            if (loadedScenes.Contains(scene)) { return; }

            StartCoroutine(LoadSceneAsyncInternal(scene, additive));
        }

        private IEnumerator LoadSceneAsyncInternal(string scene, bool additive)
        {
            AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(scene, additive ? LoadSceneMode.Additive : LoadSceneMode.Single);

            while (!asyncLoad.isDone)
            {
                yield return null;
            }

            loadedScenes.Add(scene);
        }

        public void UnloadSceneAsync(string scene)
        {
            if (!loadedScenes.Contains(scene)) { return; }

            StartCoroutine(UnloadSceneAsyncInternal(scene));
        }

        private IEnumerator UnloadSceneAsyncInternal(string scene)
        {
            AsyncOperation asyncUnload = SceneManager.UnloadSceneAsync(scene);

            while (!asyncUnload.isDone)
            {
                yield return null;
            }

            loadedScenes.Remove(scene);
        }

        private bool SceneMissing(string scene)
        {
            if (SceneManager.GetSceneByName(scene).IsValid())
            {
                return false;
            }
            else
            {
                Debug.Log($"Tried loading the scene: {scene} however it doesn t exist or isn t registered");
                return true;
            }
        }
    }
}