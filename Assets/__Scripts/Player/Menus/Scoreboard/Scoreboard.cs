using GameManagement;
using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;

namespace Menus
{
    public sealed class Scoreboard : MonoBehaviour
    {
        private PlayerScoreboardInfos[] players;
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

            players = new PlayerScoreboardInfos[GameNetworkManager.Manager.PlayerCount];

            for (int index = 0; index < GameNetworkManager.Manager.PlayerCount; index++)
            {
                ref PlayerFrame playerFrame = ref GameNetworkManager.Manager.Players[index];
                playerFrame.ScoreboardIndex = index;
                players[index] = new PlayerScoreboardInfos(playerFrame.TeamNumber, playerFrame.Name.ToString());
            }
        }

        public void AddKill(int tryharderIndex)
        {
            players[tryharderIndex].Kills++;
        }

        public void AddDeath(int deadGuyIndex)
        {
            players[deadGuyIndex].Deaths++;
        }

        public void AddAssist(int KSedIndividual)
        {
            players[KSedIndividual].Deaths++;
        }

        public void SetDoRender(bool doRender_)
        {
            doRender = doRender_;
        }

        private void OnGUI()
        {
            if (!doRender) { return; }

            GUILayout.BeginHorizontal(CachedGUIStylesNames.Box);

            GUILayout.Label("W content");

            GUILayout.EndHorizontal();
        }
    }
}