#define DEV_BUILD

using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;

namespace SceneHandling
{
    public sealed class SceneLoader : MonoBehaviour // perhaps add a callback for some Init when the map is loaded;
    {
        public static SceneLoader Instance;

        private readonly List<string> loadedScenes = new();
        private readonly List<string> loadedHUDs = new();

        #region GUI variables

        public string SceneName;
        public SceneType SceneType;

        #endregion

        private void Awake()
        {
            Instance = this;
        }

#if DEV_BUILD

        [MenuItem("Developer/Debug/_LaunchDebugOverlay")]
        public static void StaticLoadLoadDebugOverlay()
        {
            Instance.LoadDebugOverlay();
        }

        private void LoadDebugOverlay()
        {
            LoadScene(Scenes.HUD.DebugOverlay, SceneType.HUD);
        }
#endif


        [MenuItem("Developer/ScenesHandling/DebugLoadedScenes")]
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

        public static void LoadScene(string scenePath, SceneType sceneType)
        {
            Instance.LoadSceneInternal(scenePath, sceneType);
        }

        private void LoadSceneInternal(string scenePath, SceneType sceneType)
        {
            List<string> relevantSceneList = GetRelevantSceneList(sceneType);

            if (relevantSceneList.Contains(scenePath)) { return; }

            StartCoroutine(LoadSceneAsyncInternal(scenePath, relevantSceneList));
        }

        private IEnumerator LoadSceneAsyncInternal(string scene, List<string> relevantSceneList)
        {
            AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(scene, LoadSceneMode.Additive);

            while (!asyncLoad.isDone) 
            {
                yield return null;
            }

            relevantSceneList.Add(scene);
        }

        public static void UnloadScene(string scene, SceneType sceneType)
        {
            Instance.UnloadSceneInternal(scene, sceneType);
        }

        private void UnloadSceneInternal(string scene, SceneType sceneType)
        {
            List<string> relevantSceneList = GetRelevantSceneList(sceneType);

            if (!relevantSceneList.Contains(scene)) { return; }

            StartCoroutine(UnloadSceneAsyncInternal(scene, relevantSceneList));
        }

        private IEnumerator UnloadSceneAsyncInternal(string scene, List<string> relevantSceneList)
        {
            AsyncOperation asyncUnload = SceneManager.UnloadSceneAsync(scene);

            while (!asyncUnload.isDone)
            {
                yield return null;
            }

            relevantSceneList.Remove(scene);
        }

        private List<string> GetRelevantSceneList(SceneType sceneType)
        {
            return sceneType switch
            {
                SceneType.Map => loadedScenes,
                SceneType.HUD => loadedHUDs,
                _ => throw new Exception($"This SceneType ({sceneType}) does not exist"),
            };
        }
    }


    public enum SceneType
    {
        Map,
        HUD
    }
}