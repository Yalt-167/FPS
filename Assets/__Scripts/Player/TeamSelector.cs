using System;
using System.Collections;
using System.Collections.Generic;

using Unity.Netcode;
using UnityEngine;

using GameManagement;
using System.Linq;
using UnityEngine.Rendering;
using Unity.Services.Lobbies.Models;

public sealed class TeamSelector : NetworkBehaviour
{
    private static readonly string Team1 = "Team 1";
    private static readonly string Team2 = "Team 2";

    private static readonly Rect areaRect = new(10, 10, 300, 300);
    private string[][] teams;
    [SerializeField] private ushort maxTeamSize = 6;

    private bool active;

    private int[] teamsIndex = new int[2] {0, 0 };

    private void Awake()
    {
        active = true;
        teams = new string[2][] { new string[maxTeamSize], new string[maxTeamSize] };
    }


    [SerializeField] private Transform redTeamHeader;
    [SerializeField] private Transform blueTeamHeader;

    [SerializeField] private Transform wholeTeamSelectionMenu;

    //private void OnGUI()
    //{
    //    GUILayout.BeginArea(areaRect);

    //    if (GUILayout.Button(Team1)) { OnTeamSelected(1); }
    //    if (GUILayout.Button(Team2)) { OnTeamSelected(2); }

    //    GUILayout.EndArea();
    //}

    private void OnGUI()
    {
        GUILayout.Label($"MX: {Input.mousePosition}");
    }

    private void Update()
    {
        GetComponent<PlayerFrame>().ToggleCursor(towardOn: true);

        if (!active) { return; }


        if (Input.GetMouseButtonDown(0))
        {
            Debug.Log($"MX: {Input.mousePosition}, Half: {Screen.width / 2}");
            OnTeamSelected((ushort)(Input.mousePosition.x < Screen.width / 2 ? 1 : 2));
        }
    }

    private void OnTeamSelected(ushort teamID)
    {
        var playerFrame = GetComponent<PlayerFrame>();

        playerFrame.RequestSetTeamServerRpc(teamID);
        AddPlayerToTeamServerRpc(playerFrame.Name.ToString(), teamID);

        //active = false;
    }

    [Rpc(SendTo.Server)]
    private void AddPlayerToTeamServerRpc(string player, ushort teamID)
    {
        int teamIndex = teamID - 1; 
        if (teamsIndex[teamIndex] > maxTeamSize - 1)
        {
            Debug.Log("Cannot join this team because it s full");
            return;
        }

        AddPlayerToTeamClientRpc(player, teamIndex);
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void AddPlayerToTeamClientRpc(string player, int teamIndex)
    {
        if (teams[teamIndex].Contains(player))
        {
            Debug.Log("You have already joined this team");
            return;
        }

        var otherTeamIdx = teamIndex == 0 ? 1 : 0;
        if (teams[otherTeamIdx].Contains(player))
        {
            ShiftPlayerList(otherTeamIdx, player);
        }

        teams[teamIndex][teamsIndex[teamIndex]] = player;

        (teamIndex == 0 ? redTeamHeader : blueTeamHeader).GetChild(teamsIndex[teamIndex]).GetComponent<TMPro.TextMeshProUGUI>().text = player;

        teamsIndex[teamIndex]++;

        //active = false;
        //wholeTeamSelectionMenu.gameObject.SetActive(false);
    }

    private void ShiftPlayerList(int teamIndex, string nameToRemove)
    {
        string[] newArray = new string[maxTeamSize];

        int newArrayIndex = 0;
        for (int i = 0; i < maxTeamSize; i++)
        {
            var current = teams[teamIndex][i];
            if (!string.IsNullOrEmpty(current))
            {
                if (current != nameToRemove)
                {
                    newArray[newArrayIndex++] = current;
                }
            }
        }

        teams[teamIndex] = newArray;
        teamsIndex[teamIndex] = newArrayIndex;

        UpdateTeamDisplay();
    }

    private void UpdateTeamDisplay()
    {
        Transform relevantTransform = null;
        for (int teamIndex = 0; teamIndex < teams.Length; teamIndex++)
        {
            relevantTransform = teamIndex == 0 ? redTeamHeader : blueTeamHeader;
            for (int playerIndex = 0;  playerIndex < maxTeamSize; playerIndex++)
            {
                Debug.Log("Looped");
                relevantTransform.GetChild(playerIndex).GetComponent<TMPro.TextMeshProUGUI>().text = teams[teamIndex][playerIndex];
            }
        }
    }
}
