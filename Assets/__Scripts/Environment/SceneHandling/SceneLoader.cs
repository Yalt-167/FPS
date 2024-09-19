#define DEV_BUILD

using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;

namespace SceneHandling
{
    public sealed class SceneLoader : MonoBehaviour // perhaops add a callback for some Init when the map is loaded;
    {
        public static SceneLoader Instance;

        private readonly List<string> loadedScenes = new();

        #region GUI variables

        public string SceneName;
        public bool Additive;

        #endregion

        private void Awake()
        {
            Instance = this;
#if DEV_BUILD
            LoadDebugOverlay();

#endif
        }

#if DEV_BUILD
        private void LoadDebugOverlay()
        {
            LoadScene("_Scenes/DebugOverlay", true);
        }

#endif


        [MenuItem("Developer/DebugLoadedScenes")]
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
            if (loadedScenes.Contains(scenePath)) { return; }

            if (!additive)
            {
                loadedScenes.Clear();
            }
            StartCoroutine(LoadSceneAsyncInternal(scenePath, additive));

#if DEV_BUILD
            if (!additive)
            {
                LoadDebugOverlay();
            }
#endif
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