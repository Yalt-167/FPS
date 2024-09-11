using System;
using System.Collections;
using System.Collections.Generic;

using UnityEditor;
using UnityEngine;

namespace MyDebug
{
    [DefaultExecutionOrder(int.MaxValue)]
    public sealed class DebugOSD : MonoBehaviour
    {
        public static DebugOSD Instance { get; private set; }

        [SerializeField] private bool active;
        private Vector4 debugRect = new Vector4(5, 5, 500, 500);
        private readonly Dictionary<string, string> debuggerEntries = new Dictionary<string, string>();


        private void Awake()
        {
            Instance = this;
        }

        private void Update()
        {
            active = Input.GetKeyDown(KeyCode.F3) ? !active : active;
        }

        private void OnGUI()
        {
            if (!active) { return; }

            GUI.BeginGroup(debugRect.ToRect());
            GUILayout.BeginVertical(GUI.skin.box);

            if (debuggerEntries.Count == 0)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label("No tracked value RN");
                GUILayout.EndHorizontal();
            }
            else
            {
                foreach (KeyValuePair<string, string> entry in debuggerEntries)
                {
                    GUILayout.BeginHorizontal();
                    GUILayout.Label($"{entry.Key}: {entry.Value}");
                    GUILayout.EndHorizontal();
                }
            }

            GUILayout.EndVertical();
            GUI.EndGroup();
        }

        public static void Queue(object key, object value)
        {
            Debug.Log(Instance);

            if (Instance == null) { return; }

            Instance.QueueInternal(key.ToString(), value.ToString());
        }

        public void QueueInternal(string key, string value)
        {
            if (!active) { return; }

            Debug.Log("Queued");

            debuggerEntries[key] = value;
        }
    }
}