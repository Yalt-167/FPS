using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;

using GameManagement;
using Unity.VisualScripting;

namespace Menus
{
    public sealed class MainScoreboard : MonoBehaviour
    {
        public static MainScoreboard Instance { get; private set; }

        private int[] scores;
        private bool active;
        private bool fullInit;

        private int widthPerTeam;
        //private int spaceBetweenTeamDisplay = 20;

        private GUIStyle centeredLabelStyle;


        public static void Toggle(bool towardOn)
        {
            Instance.ToggleInternal(towardOn);
        }

        private void ToggleInternal(bool towardOn)
        {
            active = towardOn;
        }

        private void Awake()
        {
            Game.OnGeneralGameStartedClientSide += Init;
        }

        private void OnDisable()
        {
            Game.OnGeneralGameStartedClientSide -= Init;
        }

        private void Init()
        {
            Instance = this;
            scores = new int[GameNetworkManager.Manager.TeamSelectionScreen.TeamsCount];
            active = true;

            widthPerTeam = 600 / scores.Length;
        }

        private void OnGUI()
        {
            if (!active) { return; }

            if (!fullInit)
            {
                centeredLabelStyle = GUI.skin.label;
                centeredLabelStyle.alignment = TextAnchor.MiddleCenter;
                fullInit = true;
            }

            GUILayout.BeginHorizontal(GUILayout.Width(Screen.width));
            GUILayout.FlexibleSpace();

            GUILayout.BeginHorizontal(CachedGUIStylesNames.Box, GUILayout.Width(600));

            for (int i = 0; i < scores.Length; i++)
            {
                GUILayout.Label($"{scores[i]}", centeredLabelStyle,  GUILayout.Width(widthPerTeam));
            }

            GUILayout.EndHorizontal();

            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
        }



        public static void AddScore(int teamNumber, int scoreToAdd)
        {
            Instance.scores[teamNumber - 1] = scoreToAdd;
        }
    }
}