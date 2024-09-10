#define DEV_BUILD

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
        private readonly List<int> loadedScenesIndexes = new();

        #region GUI variables

        public string SceneName;
        public bool Additive;

        #endregion

        private void Awake()
        {
            Instance = this;
#if DEV_BUILD
            LoadScene("_Scenes/DebugOverlay", true);
#endif
        }

        public static void DebugLoadedScenes()
        {
            var sceneCount = SceneManager.sceneCount;
            Debug.Log(sceneCount);
            for (int sceneIndex = 0; sceneIndex < sceneCount; sceneIndex++)
            {
                var scene = SceneManager.GetSceneAt(sceneIndex);
                Debug.Log(scene.name);
                Debug.Log(scene.path);
            }
        }

        public void LoadScene(string scenePath, bool additive)
        {
            //if (SceneInvalid(scenePath)) { return; }

            if (loadedScenes.Contains(scenePath)) { return; }

            StartCoroutine(LoadSceneAsyncInternal(scenePath, additive));
        }
        public void LoadScene(int sceneIndex, bool additive)
        {
            //if (SceneMissing(sceneIndex)) { return; }

            if (loadedScenesIndexes.Contains(sceneIndex)) { return; }

            StartCoroutine(LoadSceneAsyncInternal(sceneIndex, additive));
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
        private IEnumerator LoadSceneAsyncInternal(int sceneIndex, bool additive)
        {
            AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneIndex, additive ? LoadSceneMode.Additive : LoadSceneMode.Single);

            while (!asyncLoad.isDone)
            {
                yield return null;
            }

            loadedScenesIndexes.Add(sceneIndex);
        }

        public void UnloadScene(string scene)
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
    }
}