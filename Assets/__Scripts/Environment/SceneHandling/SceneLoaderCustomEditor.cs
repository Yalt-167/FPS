using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace SceneHandling
{
    [CustomEditor(typeof(SceneLoader))]
    public sealed class SceneLoaderCustomEditor : Editor
    {
        private SceneLoader sceneLoader;
        private void Awake()
        {
            sceneLoader = (SceneLoader)target;
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            if (GUILayout.Button("Try loading scene"))
            {
                SceneLoader.LoadScene(sceneLoader.SceneName, sceneLoader.SceneType);
            }

            if (GUILayout.Button("Load Debug Overlay"))
            {
                SceneLoader.LoadScene(Scenes.HUD.DebugOverlay, SceneType.HUD);
            }
        }
    }
}