using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;

using GameManagement;
using Inputs;


namespace Menus
{
    public sealed class FoldableScoreboard : MonoBehaviour
    {
        public static FoldableScoreboard Instance { get; private set; }
        private InputManager inputManager;

        private GeneralInputQuery InputQuery => inputManager.GeneralInputs;
        private PlayerScoreboardInfos[] playersInfos;
        private readonly Color[] teamColors = new Color[2] {Color.red, Color.green };

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

        private ScoreboardSortMode sortMode = ScoreboardSortMode.Kills;
        private static readonly int sortModesCount = Enum.GetValues(typeof(ScoreboardSortMode)).Length;

        private void Awake()
        {
            inputManager = GetComponent<InputManager>();
            Game.OnGeneralGameStartedClientSide += Init;
        }

        private void OnDisable()
        {
            Game.OnGeneralGameStartedClientSide -= Init;
        }

        private void Update()
        {
            if (InputQuery.CycleScoreboardSortMode)
            {
                CycleSortMode();
            }
        }

        private void Init()
        {
            Instance = this;

            var simulatedEntries = new string[] { "Diegocardi", "Syn", "Kry", "Asyl"};

            playersInfos = new PlayerScoreboardInfos[Game.PlayerCount + simulatedEntries.Length];

            for (int index = 0; index < Game.PlayerCount; index++)
            {
                ref PlayerFrame playerFrame = ref Game.Players[index];
                playerFrame.ScoreboardIndex = index;
                playersInfos[index] = new PlayerScoreboardInfos(playerFrame.TeamNumber, playerFrame.Name);
            }

            CreateFakeEntries(simulatedEntries);

            CreateFakeData();

            SortPlayers();
        }

        private void CreateFakeEntries(string[] entries)
        {
            for (int index = 0; index < entries.Length; index++)
            {
                playersInfos[Game.PlayerCount + index] = new PlayerScoreboardInfos((Game.PlayerCount + index) % 2 + 1, entries[index]);
            }
        }

        private void CreateFakeData()
        {
            for (int i = 0; i < 100; i++)
            {
                var killerIndex = UnityEngine.Random.Range(0, playersInfos.Length);
                AddKill(killerIndex);

                int victimIndex;
                do
                {
                    victimIndex = UnityEngine.Random.Range(0, playersInfos.Length);
                } while (victimIndex == killerIndex);
                AddDeath(victimIndex);

                if (UnityEngine.Random.Range(0, 2) == 1)
                {
                    int assistIndex;
                    do
                    {
                        assistIndex = UnityEngine.Random.Range(0, playersInfos.Length);
                    } while (victimIndex == killerIndex || victimIndex == assistIndex);
                    AddAssist(assistIndex);
                }
            }
        }


        private void SortPlayers()
        {
            switch (SortMode)
            {
                case ScoreboardSortMode.Teams:
                    SortPlayersPerTeams();
                    break;

                case ScoreboardSortMode.Kills:
                    SortPlayersPerKills();
                    break;

                case ScoreboardSortMode.Ratio:
                    SortPlayersPerRatio();
                    break;

                case ScoreboardSortMode.Points:
                    //SortPlayersPerPoints();
                    break;

                default:
                    throw new Exception($"This scoreboard sort mode: {SortMode} doesn t exist");
            }
        }

        private void SortPlayersPerTeams()
        {
            var teams = new List<PlayerScoreboardInfos>[2] { new List<PlayerScoreboardInfos>(), new List<PlayerScoreboardInfos>() };

            foreach (var player in playersInfos)
            {
                InsertAsSortedPerKills(player, teams[player.Team - 1]);
            }

            var (localPlayerTeam, opponentTeam) = PlayerFrame.LocalPlayer.TeamNumber == 1 ? (0, 1) : (1, 0);

            var localPlayerTeamCount = teams[localPlayerTeam].Count;
            var total = localPlayerTeamCount + teams[opponentTeam].Count;
            for (int i = 0; i < total; i++)
            {
                playersInfos[i] = i < localPlayerTeamCount ? teams[0][i] : teams[1][i - localPlayerTeamCount];
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

            list.Add(playerInfos);
        }

        private void SortPlayersPerKills()
        {
            Array.Sort(playersInfos, new PlayerScoreboardInfoPerKillsComparer());
        }

        private void SortPlayersPerRatio()
        {
            Array.Sort(playersInfos, new PlayerScoreboardInfoPerRatioComparer());
        }

        private void SortPlayersPerPoints()
        {
            throw new NotImplementedException();
        }

        private void CycleSortMode()
        {
            SortMode = (ScoreboardSortMode)(((int)sortMode + 1) % sortModesCount);
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