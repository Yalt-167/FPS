#define I_COULD_VERY_WELL_BE_RETARDED

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

using Unity.Netcode;
using UnityEngine;

using UnityEngine.UI;

using TMPro;

using GameManagement;



public sealed class TeamSelector : NetworkBehaviour
{
    private string[][] teams;
    [SerializeField] private ushort maxTeamSize = 6;

    private bool active;

    private readonly int[] teamsIndex = new int[2] {0, 0 };


    private Transform teamOneHeader;
    private Transform teamTwoHeader;
    private Button startGameButton;

    private void Awake()
    {
        active = true;
        teams = new string[2][] { new string[maxTeamSize], new string[maxTeamSize] };

        teamOneHeader = transform.GetChild(1);
        teamOneHeader.GetComponent<Button>().onClick.AddListener(JoinTeamOne);

        teamTwoHeader = transform.GetChild(2);
        teamTwoHeader.GetComponent<Button>().onClick.AddListener(JoinTeamTwo);


        startGameButton = transform.GetChild(3).GetComponent<Button>();
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
    }

    private void Update()
    {
        if (!active) { return; }

        PlayerFrame.LocalPlayer.ToggleCursor(towardOn: active);
        PlayerFrame.LocalPlayer.ToggleGameControls(towardOn: !active);

        if (Input.GetMouseButtonDown(0))
        {
            OnTeamSelected((ushort)(Input.mousePosition.x < Screen.width / 2 ? 1 : 2));
        }

        if (!PlayerFrame.LocalPlayer.IsHost) { return; }

        if (Input.GetKeyDown(KeyCode.Return))
        {
            Game.StaticStartGame();
            DisableTeamSelectionScreenServerRpc();
        }
    }

    public void JoinTeamOne()
    {
        OnTeamSelected(1);
    }

    public void JoinTeamTwo()
    {
        OnTeamSelected(2);
    }

    private void OnTeamSelected(ushort teamID)  
    {
        AddPlayerToTeamServerRpc(PlayerFrame.LocalPlayer.Name.ToString(), teamID);
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
            PlayerFrame.LocalPlayer.SetTeam((ushort)(teamIndex - 1)); // just in case
            return;
        }

        var otherTeamIdx = teamIndex == 0 ? 1 : 0;
        if (teams[otherTeamIdx].Contains(player))
        {
            RemovePlayerFromTeamList(otherTeamIdx, player);
        }

        teams[teamIndex][teamsIndex[teamIndex]] = player;

        (teamIndex == 0 ? teamOneHeader : teamTwoHeader).GetChild(teamsIndex[teamIndex]).GetComponent<TextMeshProUGUI>().text = player;

        teamsIndex[teamIndex]++;

        PlayerFrame.LocalPlayer.SetTeam((ushort)(teamIndex - 1));
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

        UpdateTeamDisplay();
    }

    private void UpdateTeamDisplay()
    {
        Transform relevantTransform;
        for (int teamIndex = 0; teamIndex < teams.Length; teamIndex++)
        {
            relevantTransform = teamIndex == 0 ? teamOneHeader : teamTwoHeader;
            for (int playerIndex = 0;  playerIndex < maxTeamSize; playerIndex++)
            {
                relevantTransform.GetChild(playerIndex).GetComponent<TextMeshProUGUI>().text = teams[teamIndex][playerIndex];
            }
        }
    }

    [Rpc(SendTo.Server)]
    private void DisableTeamSelectionScreenServerRpc()
    {
        DisableTeamSelectionScreenClientRpc();
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void DisableTeamSelectionScreenClientRpc()
    {
        gameObject.SetActive(false);
        PlayerFrame.LocalPlayer.ToggleCursor(towardOn: false);
        PlayerFrame.LocalPlayer.ToggleGameControls(towardOn: true);
    }
}
