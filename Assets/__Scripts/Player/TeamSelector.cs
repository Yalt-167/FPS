#define DEV_BUILD

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

using Unity.Netcode;
using UnityEngine;

using GameManagement;



public sealed class TeamSelector : NetworkBehaviour
{
    public static TeamSelector Instance { get; private set; }
    private string[][] teams;
    [SerializeField] private ushort teamsCount;
    [SerializeField] private ushort maxTeamSize;
    [SerializeField] private int teamSelectionMenuWidth;
    [SerializeField] private int teamSelectionMenuPlayerRegionHeightPerExpectedPlayer;
    private int[] teamsIndex;

    private bool teamSelectorMenuActive;

    private void Awake()
    {
        Instance = this;
    }

    [Rpc(SendTo.Server)]
    public void SetDataServerRpc(ushort teamsCount_, ushort teamsSize_)
    {
        SetDataClientRpc(teamsCount_, teamsSize_);
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void SetDataClientRpc(ushort teamsCount_, ushort teamsSize_)
    {
        teamsCount = teamsCount_;
        maxTeamSize = teamsSize_;

        UpdateData();
    }

    private void UpdateData()
    {
        teamSelectorMenuActive = true;
        teams = new string[teamsCount][];
        teamsIndex = new int[teamsCount];
        for (int i = 0; i < teamsCount; i++)
        {
            teams[i] = new string[maxTeamSize];
            teamsIndex[i] = 0;
        }
    }

    private bool AllPlayersAreInATeam()
    {
        var playersInTeams = 0;
        for (int teamIndex = 0; teamIndex < teamsCount; teamIndex++)
        {
            for (int teamSlotIndex = 0; teamSlotIndex < Game.PlayerCount; teamSlotIndex++)
            {
                if (string.IsNullOrEmpty(teams[teamIndex][teamSlotIndex])) { break; }
                
                playersInTeams++;
            }
        }

        return playersInTeams == Game.PlayerCount;
    }

    private void CreateTeamMenu(int teamNumber)
    {
        GUILayout.BeginVertical(CachedGUIStylesNames.Box); // V

            GUILayout.Label($"Team{teamNumber}");

            GUILayout.BeginHorizontal(CachedGUIStylesNames.Box, GUILayout.Width(teamSelectionMenuWidth)); // VH

                if (GUILayout.Button(TeamSelectorGUILabels.JoinTeam) || Input.GetKeyDown((KeyCode)(teamNumber + (int)KeyCode.Alpha0)) || Input.GetKeyDown((KeyCode)(teamNumber + (int)KeyCode.Keypad0)))
                {
                    OnTeamSelected((ushort)teamNumber);
                }

            GUILayout.EndHorizontal(); // V

            GUILayout.BeginVertical(CachedGUIStylesNames.Box, GUILayout.Height(teamSelectionMenuPlayerRegionHeightPerExpectedPlayer * maxTeamSize)); // VV

                foreach (var playerName in teams[teamNumber - 1])
                {
                    GUILayout.Label(playerName);
                }

            GUILayout.EndVertical(); // V
         
        GUILayout.EndVertical(); //
    }

    private void OnGUI()
    {
        if (!teamSelectorMenuActive) { return; }

        GUILayout.BeginVertical(GUILayout.Height(Screen.height));

        GUILayout.FlexibleSpace();

        GUILayout.BeginHorizontal(GUILayout.Width(Screen.width));

        GUILayout.FlexibleSpace();

        for (int i = 0; i < teamsCount; i++)
        {
            GUILayout.BeginVertical(CachedGUIStylesNames.Box);

                CreateTeamMenu(i + 1);

            GUILayout.EndVertical();

            GUILayout.FlexibleSpace();
        }

        GUILayout.EndHorizontal();

        GUILayout.FlexibleSpace();

        if (LobbyHandling.LobbyHandler.Instance.IsLobbyHost()
#if DEV_BUILD
||  LobbyHandling.LobbyHandler.Instance.UsingLocalTestingSession
#endif
            )
        {
            GUILayout.BeginHorizontal(GUILayout.Width(Screen.width));

            GUILayout.FlexibleSpace();

            if (AllPlayersAreInATeam() && (GUILayout.Button(TeamSelectorGUILabels.StartGame) || Input.GetKeyDown(KeyCode.Return)))
            {
                ToggleTeamSelectionScreenServerRpc(towardOn__: false);
                Game.StartGame();
            }

            GUILayout.FlexibleSpace();

            GUILayout.EndHorizontal();
        }

        GUILayout.EndVertical();
    }

    private void OnTeamSelected(ushort teamNumber)
    {
        AddPlayerToTeamServerRpc(PlayerFrame.LocalPlayer.Name, teamNumber);
    }

    [Rpc(SendTo.Server)]
    private void AddPlayerToTeamServerRpc(string player, ushort teamNumber)
    {
        int teamIndex = teamNumber - 1; 
        if (teamsIndex[teamIndex] > maxTeamSize - 1)
        {
            Debug.Log("Cannot join this team because it s full already");
            return;
        }

        AddPlayerToTeamClientRpc(player, teamIndex);
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void AddPlayerToTeamClientRpc(string player, int teamIndex)
    {
        if (teams[teamIndex].Contains(player)) { return; }

        var otherTeamIndex = teamIndex == 0 ? 1 : 0;
        if (teams[otherTeamIndex].Contains(player))
        {
            RemovePlayerFromTeamList(otherTeamIndex, player);
        }

        teams[teamIndex][teamsIndex[teamIndex]] = player;

        teamsIndex[teamIndex]++;

        Game.GetPlayerFromName(player).SetTeam((ushort)(teamIndex + 1));
    }

    private void RemovePlayerFromTeamList(int teamIndex, string nameToRemove)
    {
        string[] newArray = new string[maxTeamSize];

        int newArrayIndex = 0;
        for (int i = 0; i < maxTeamSize; i++)
        {
            var current = teams[teamIndex][i];
            if (string.IsNullOrEmpty(current))
            {
                break;
            }

            if (current != nameToRemove)
            {
                newArray[newArrayIndex++] = current;
            }
        }

        teams[teamIndex] = newArray;
        teamsIndex[teamIndex] = newArrayIndex;
    }

    [Rpc(SendTo.Server)]
    public void ToggleTeamSelectionScreenServerRpc(bool towardOn__)
    {
        ToggleTeamSelectionScreenClientRpc(towardOn_: towardOn__);
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void ToggleTeamSelectionScreenClientRpc(bool towardOn_)
    {
        ToggleSelectionScreenMenu(towardOn: towardOn_);

        PlayerFrame.LocalPlayer.ToggleCursor(towardOn: towardOn_);

        PlayerFrame.LocalPlayer.ToggleGameControls(towardOn: !towardOn_);
        PlayerFrame.LocalPlayer.ToggleCamera(towardOn: !towardOn_);
        PlayerFrame.LocalPlayer.ToggleHUD(towardOn: !towardOn_);
    }

    private void ToggleSelectionScreenMenu(bool towardOn)
    {
        teamSelectorMenuActive = towardOn;
    }


    public static class TeamSelectorGUILabels
    {
        public static readonly string StartGame = "Start game";
        public static readonly string JoinTeam = "Join team";
    }
}
