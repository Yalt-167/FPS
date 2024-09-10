using System;
using System.Collections;
using System.Collections.Generic;

using UnityEditor;
using UnityEngine;

namespace MyDebug
{
    [DefaultExecutionOrder(int.MaxValue)]
    public sealed class OnScreenInfoDisplay : MonoBehaviour
    {
        public static OnScreenInfoDisplay Instance { get; private set; }

        [SerializeField] private bool active;
        [SerializeField] private Vector4 debugRect;
        [SerializeField] private int indentWidth;
        [SerializeField] private int verticalMarging;
        private readonly Dictionary<string, string> debuggerEntries = new Dictionary<string, string>();


        private void Awake()
        {
            Instance = this;
        }

        private void Update()
        {
            active = Input.GetKeyDown(KeyCode.F3) ? !active : active;
        }

        private void LateUpdate()
        {
            debuggerEntries.Clear();
        }


        private void OnGUI()
        {
            if (!active) { return; }

            GUI.BeginGroup(debugRect.ToRect());
            GUILayout.BeginVertical(GUI.skin.box);
            GUILayout.Space(indentWidth);
            GUILayout.Label("W Content");
            GUILayout.Space(indentWidth);
            GUILayout.Label("W Content");
            foreach (KeyValuePair<string, string> entry in debuggerEntries)
            {
                GUILayout.Label($"{entry.Key}: {entry.Value}");
            }

            GUILayout.EndVertical();
            GUI.EndGroup();
        }

        public static void Debug(string key, string value)
        {
            if (Instance == null) { return; }

            Instance.DebugInternal(key, value);
        }

        public void DebugInternal(string key, string value)
        {
            if (!active) { return; }

            debuggerEntries[key] = value;
        }
    }
}