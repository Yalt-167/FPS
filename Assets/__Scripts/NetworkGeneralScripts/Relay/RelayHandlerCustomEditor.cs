using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using LobbyHandling;

namespace RelayHandling
{
    [CustomEditor(typeof(RelayHandler))]
    public sealed class RelayHandlerCustomEditor : Editor
    {
        private RelayHandler targetScript;
        private static readonly int spaceBetweenButtons = 4;

        public void OnEnable()
        {
            targetScript = (RelayHandler)target;
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            if (GUILayout.Button("Create relay"))
            {
                CreateRelayWithoutAwaiting();
            }

            GUILayout.Space(spaceBetweenButtons);
            if (GUILayout.Button("Join relay"))
            {
                targetScript.JoinRelay(targetScript.JoinCode);
            }
        }


        private async void CreateRelayWithoutAwaiting()
        {
            var joinCode = await targetScript.CreateRelay(targetScript.Slots, targetScript.LaunchAsHost);
            if (!string.IsNullOrEmpty(joinCode))
            {
                Debug.Log(joinCode);
            }
        }
    } 
}
