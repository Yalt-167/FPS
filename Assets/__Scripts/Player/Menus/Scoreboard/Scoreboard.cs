using GameManagement;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Menus
{
    public sealed class Scoreboard : MonoBehaviour
    {
        private PlayerScoreboardInfos[] playersInfos;
        public static Scoreboard Instance { get; private set; }

        private bool doRender;

        private void Awake()
        {
            Game.OnGameStarted += Init;
        }

        private void OnDisable()
        {
            Game.OnGameStarted -= Init;
        }

        private void Init()
        {
            Instance = this;
            var simulatedEntries = new string[] { "Diegocardi", "Syn", "Kry", "Asyd"};
            playersInfos = new PlayerScoreboardInfos[GameNetworkManager.Manager.PlayerCount + simulatedEntries.Length];

            for (int index = 0; index < GameNetworkManager.Manager.PlayerCount; index++)
            {
                ref PlayerFrame playerFrame = ref GameNetworkManager.Manager.Players[index];
                playerFrame.ScoreboardIndex = index;
                playersInfos[index] = new PlayerScoreboardInfos(playerFrame.TeamNumber, playerFrame.Name.ToString());
            }

            CreateFakeEntries(simulatedEntries);
        }

        private void CreateFakeEntries(string[] entries)
        {
            for (int index = 0; index < entries.Length; index++)
            {
                playersInfos[GameNetworkManager.Manager.PlayerCount + index] = new PlayerScoreboardInfos(index % 2 + 1, entries[index]);
            }
        }

        public void AddKill(int tryharderIndex)
        {
            playersInfos[tryharderIndex].Kills++;
        }

        public void AddDeath(int deadGuyIndex)
        {
            playersInfos[deadGuyIndex].Deaths++;
        }

        public void AddAssist(int KSedIndividual)
        {
            playersInfos[KSedIndividual].Deaths++;
        }

        public void SetDoRender(bool doRender_)
        {
            doRender = doRender_;
        }

        private void OnGUI()
        {
            if (!doRender) { return; }

            GUILayout.BeginHorizontal(GUILayout.Width(Screen.width));

            GUILayout.FlexibleSpace();

            GUILayout.BeginVertical(CachedGUIStylesNames.Box, GUILayout.Width(600));

            DrawScoreboardHeader();

            foreach (var playerInfos in playersInfos)
            {
                DrawPlayerSection(playerInfos);
            }

            GUILayout.EndVertical();

            GUILayout.FlexibleSpace();

            GUILayout.EndHorizontal();
        }

        private void DrawScoreboardHeader()
        {
            GUI.color = Color.black;
            
            GUILayout.BeginHorizontal(CachedGUIStylesNames.Box);

            GUILayout.Label("Players", GUILayout.Width(200));
            GUILayout.Label("Champions", GUILayout.Width(200));
            GUILayout.Label("K/D/A", GUILayout.Width(200));

            GUILayout.EndHorizontal();

            GUI.color = Color.white;
        }

        private void DrawPlayerSection(PlayerScoreboardInfos playerInfos)
        {
            GUILayout.BeginHorizontal(CachedGUIStylesNames.Box);

            GUILayout.Label(playerInfos.Name, GUILayout.Width(200));
            GUILayout.Label(playerInfos.CurrentChampion, GUILayout.Width(200));
            GUILayout.Label($"{playerInfos.Kills}/{playerInfos.Deaths}/{playerInfos.Assists}", GUILayout.Width(200));

            GUILayout.EndHorizontal();
        }
    }
}
// so far when the Gravity relies on PlayerMovement which is deactivated when a menu is opened