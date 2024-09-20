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
                sceneLoader.LoadScene(sceneLoader.SceneName, sceneLoader.Additive);
            }

            if (GUILayout.Button("Load Debug Overlay"))
            {
                sceneLoader.LoadScene("_Scenes/DebugOverlay", true);
            }
        }
    }
}