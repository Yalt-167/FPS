using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;

namespace Menus
{
    public sealed class MainScoreboard : MonoBehaviour
    {
        public static MainScoreboard Instance { get; private set; }

        private int[] scores;
        private bool active;

        [SerializeField] private int widthPerTeam;
        [SerializeField] private int spaceBetweenTeamDisplay;
        [SerializeField] private int height;
        public static void Toggle(bool towardOn)
        {
            Instance.ToggleInternal(towardOn);
        }

        private void ToggleInternal(bool towardOn)
        {
            active = towardOn;
        }

        private void InitLocalPlayer() // called by PlayerFrame through reflection
        {
            MyDebug.DebugUtility.LogMethodCall();
            scores = new int[GameManagement.GameNetworkManager.Manager.TeamSelectionScreen.TeamsCount];
            active = true;
        }

        private void OnGUI()
        {
            if (!active) { return; }

            GUILayout.BeginHorizontal(GUILayout.ExpandWidth(true));
            GUILayout.FlexibleSpace();

            GUILayout.BeginHorizontal(GUILayout.Width(widthPerTeam * scores.Length + spaceBetweenTeamDisplay * (scores.Length - 1)));

            for (int i = 0; i < scores.Length; i++)
            {
                GUILayout.Label($"{scores[i]}");
                GUILayout.Space(spaceBetweenTeamDisplay);
            }

            GUILayout.EndHorizontal();

            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
        }
    }
}