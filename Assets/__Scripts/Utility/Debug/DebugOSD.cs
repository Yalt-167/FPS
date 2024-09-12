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

        private bool active;
        private Vector4 debugBounds = new Vector4(5, 5, 500, 500);
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

            GUI.BeginGroup(debugBounds.ToRect());
            GUILayout.BeginVertical(GUI.skin.box);

            _ = debuggerEntries.Count == 0 ? DebugEmpty() : DebugNotEmpty();

            GUILayout.EndVertical();
            GUI.EndGroup();
        }

        private object DebugEmpty()
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label("No tracked value RN");
            GUILayout.EndHorizontal();

            return null;
        }

        private object DebugNotEmpty()
        {
            foreach (KeyValuePair<string, string> entry in debuggerEntries)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label($"{entry.Key}: {entry.Value}");
                GUILayout.EndHorizontal();
            }

            return null;
        }

        public static void Display(object key, object value)
        {
            if (Instance == null) { return; }

            Instance.DisplayInternal(key.ToString(), value.ToString());
        }

        public void DisplayInternal(string key, string value)
        {
            if (!active) { return; }

            debuggerEntries[key] = value;
        }

        public static void DisplayForTime(object key, object value, float durationInSeconds)
        {
            if (Instance == null) { return; }

            Instance.DisplayForTimeInternal(key.ToString(), value.ToString(), durationInSeconds);
        }

        public static void DisplayForTime(object key, object value, DisplayDurationTypesInMS duration)
        {
            if (Instance == null) { return; }

            Instance.DisplayForTimeInternal(key.ToString(), value.ToString(), (int)duration / 1000);
        }

        public void DisplayForTimeInternal(string key, string value, float durationInSeconds)
        {
            if (!active) { return; }

            debuggerEntries[key] = value;
            StartCoroutine(QueueDataDeletion(key, durationInSeconds));
        }

        private IEnumerator QueueDataDeletion(string dataKey, float duration)
        {
            yield return new WaitForSeconds(duration);

            debuggerEntries.Remove(dataKey);
        }
        public enum DisplayDurationTypesInMS
        {
            Brief = 1200,
            Long = 2400
        }
    }

}