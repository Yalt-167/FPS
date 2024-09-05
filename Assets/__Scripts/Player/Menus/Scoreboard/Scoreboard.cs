using GameManagement;
using Menus;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace Menus
{
    public sealed class Scoreboard : MonoBehaviour
    {
        public static Scoreboard Instance { get; private set; }

        private PlayerScoreboardInfos[] playersInfos;
        private Color[] teamColors = new Color[2] {Color.red, Color.blue };

        private bool doRender;

        private ScoreboardSortMode SortMode
        {
            get
            {
                return sortMode;
            }
            set
            {
                sortMode = value;
                SortPlayers();
            }
        }

        private ScoreboardSortMode sortMode = ScoreboardSortMode.Teams;

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

            var simulatedEntries = new string[] { "Diegocardi", "Syn", "Kry", "Asyl"};

            playersInfos = new PlayerScoreboardInfos[GameNetworkManager.Manager.PlayerCount + simulatedEntries.Length];

            for (int index = 0; index < GameNetworkManager.Manager.PlayerCount; index++)
            {
                ref PlayerFrame playerFrame = ref GameNetworkManager.Manager.Players[index];
                playerFrame.ScoreboardIndex = index;
                playersInfos[index] = new PlayerScoreboardInfos(playerFrame.TeamNumber, playerFrame.Name.ToString());
            }

            CreateFakeEntries(simulatedEntries);

            for (int i = 0; i < 100; i++)
            {
                var killerIndex = UnityEngine.Random.Range(0, playersInfos.Length);

                int victimIndex;
                do
                {
                    victimIndex = UnityEngine.Random.Range(0, playersInfos.Length);
                } while (victimIndex == killerIndex);

                if (UnityEngine.Random.Range(0, 2) == 1)
                {
                    int assistIndex;
                    do
                    {
                        assistIndex = UnityEngine.Random.Range(0, playersInfos.Length);
                    } while (victimIndex == killerIndex || victimIndex == assistIndex);
                    AddAssist(assistIndex);
                }
                AddKill(killerIndex);
                AddDeath(victimIndex);
            }
        }

        private void CreateFakeEntries(string[] entries)
        {
            for (int index = 0; index < entries.Length; index++)
            {
                playersInfos[GameNetworkManager.Manager.PlayerCount + index] = new PlayerScoreboardInfos((GameNetworkManager.Manager.PlayerCount + index) % 2 + 1, entries[index]);
            }
        }

        private void SortPlayers()
        {
            switch (sortMode)
            {
                case ScoreboardSortMode.Teams:
                    SortPlayersPerTeams();
                    break;

                case ScoreboardSortMode.Kills:
                    SortPlayersPerKills();
                    break;

                default:
                    throw new Exception($"This scoreboard sort mode: {sortMode} doesn t exist");
            }
        }

        private void SortPlayersPerTeams()
        {
            var teams = new List<PlayerScoreboardInfos>[2] { new List<PlayerScoreboardInfos>(), new List<PlayerScoreboardInfos>() };

            foreach (var player in playersInfos)
            {
                InsertAsSortedPerKills(player, teams[player.Team - 1]);
            }
        }

        private void InsertAsSortedPerKills(PlayerScoreboardInfos playerInfos, List<PlayerScoreboardInfos> list)
        {
            if (list.Count == 0)
            {
                list.Add(playerInfos);
                return;
            }

            for(int i = 0; i < list.Count; i++)
            {
                if (playerInfos.Kills > list[i].Kills)
                {
                    list.Insert(i, playerInfos);
                    return;
                }
            }
        }

        private void SortPlayersPerKills()
        {
            Array.Sort(playersInfos, new PlayerScoreboardInfoComparer());
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
            playersInfos[KSedIndividual].Assists++;
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
            GUILayout.BeginHorizontal(CachedGUIStylesNames.Box);

            GUILayout.Label("Players", GUILayout.Width(200));
            GUILayout.Label("Champions", GUILayout.Width(200));
            GUILayout.Label("K/D/A", GUILayout.Width(200));

            GUILayout.EndHorizontal();
        }

        private void DrawPlayerSection(PlayerScoreboardInfos playerInfos)
        {
            GUI.color = teamColors[playerInfos.Team - 1];
            GUILayout.BeginHorizontal(CachedGUIStylesNames.Box);

            GUILayout.Label(playerInfos.Name, GUILayout.Width(200));
            GUILayout.Label(playerInfos.CurrentChampion, GUILayout.Width(200));
            GUILayout.Label($"{playerInfos.Kills}/{playerInfos.Deaths}/{playerInfos.Assists}", GUILayout.Width(200));

            GUILayout.EndHorizontal();
            GUI.color = Color.white;
        }
    }
}
// so far when the Gravity relies on PlayerMovement which is deactivated when a menu is opened

public sealed class PlayerScoreboardInfoComparer : IComparer<PlayerScoreboardInfos>
{
    public int Compare(PlayerScoreboardInfos x, PlayerScoreboardInfos y)
    {
        return x.Kills.CompareTo(y.Kills);
    }
}