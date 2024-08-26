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

        public  void LoadSceneAsync(string sceneName, bool additive)
        {
            if (loadedScenes.Contains(sceneName)) { return; }

            StartCoroutine(LoadSceneAsyncInternal(sceneName, additive));
        }

        private IEnumerator LoadSceneAsyncInternal(string sceneName, bool additive)
        {
            AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneName, additive ? LoadSceneMode.Additive : LoadSceneMode.Single);

            while (!asyncLoad.isDone)
            {
                yield return null;
            }

            loadedScenes.Add(sceneName);
        }

        public void UnloadSceneAsync(string sceneName)
        {
            if (!loadedScenes.Contains(sceneName)) { return; }

            StartCoroutine(UnloadSceneAsyncInternal(sceneName));
        }

        private IEnumerator UnloadSceneAsyncInternal(string sceneName)
        {
            AsyncOperation asyncUnload = SceneManager.UnloadSceneAsync(sceneName);

            while (!asyncUnload.isDone)
            {
                yield return null;
            }

            loadedScenes.Remove(sceneName);
        }
    }
}