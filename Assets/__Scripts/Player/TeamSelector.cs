#define USING_UNITY_GUI_SYSTEM

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

using Unity.Netcode;
using UnityEngine;

using GameManagement;



public sealed class TeamSelector : NetworkBehaviour
{
    private string[][] teams;
    [SerializeField] private ushort teamsCount;
    [SerializeField] private ushort maxTeamSize;
    private int[] teamsIndex;

    private bool active;


#if USING_UNITY_GUI_SYSTEM

#else
    private Transform teamOneHeader;
    private Transform teamTwoHeader;
    private Button startGameButton;
#endif

    private void Awake()
    {
#if USING_UNITY_GUI_SYSTEM

#else
        teamOneHeader = transform.GetChild(1);
        teamOneHeader.GetComponent<Button>().onClick.AddListener(JoinTeamOne);

        teamTwoHeader = transform.GetChild(2);
        teamTwoHeader.GetComponent<Button>().onClick.AddListener(JoinTeamTwo);


        startGameButton = transform.GetChild(3).GetComponent<Button>();
#endif
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
        active = true;
        teams = new string[teamsCount][];
        teamsIndex = new int[teamsCount];
        for (int i = 0; i < teamsCount; i++)
        {
            teams[i] = new string[maxTeamSize];
            teamsIndex[i] = 0;
        }
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
    }

    private void Update()
    {
#if USING_UNITY_GUI_SYSTEM

#else
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
#endif
    }

    private void CreateTeamMenu(int teamNumber)
    {
        GUILayout.BeginVertical("box");

            GUILayout.Label($"Team{teamNumber}");

            GUILayout.BeginHorizontal("box");

                if (GUILayout.Button("Join team"))
                {
                    OnTeamSelected((ushort)teamNumber);
                }

            GUILayout.EndHorizontal();

            GUILayout.BeginVertical("box");

                foreach (var playerName in teams[teamNumber - 1])
                {
                    GUILayout.Label(playerName);
                }

            GUILayout.EndVertical();

        GUILayout.EndVertical();
    }

    private void OnGUI()
    {
        GUILayout.BeginVertical();

        GUILayout.FlexibleSpace();

        GUILayout.BeginHorizontal();

        GUILayout.FlexibleSpace();

        for (int i = 0; i < teamsCount; i++)
        {
            GUILayout.BeginVertical("box");

                CreateTeamMenu(i + 1);

            GUILayout.EndVertical();

            GUILayout.FlexibleSpace();
        }

        GUILayout.EndHorizontal();

        GUILayout.FlexibleSpace();

        GUILayout.EndVertical();
    }

    private void OnTeamSelected(ushort teamNumber)  
    {
        AddPlayerToTeamServerRpc(PlayerFrame.LocalPlayer.Name.ToString(), teamNumber);
    }

    [Rpc(SendTo.Server)]
    private void AddPlayerToTeamServerRpc(string player, ushort teamNumber)
    {
        int teamIndex = teamNumber - 1; 
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
#if USING_UNITY_GUI_SYSTEM

#else
        (teamIndex == 0 ? teamOneHeader : teamTwoHeader).GetChild(teamsIndex[teamIndex]).GetComponent<TextMeshProUGUI>().text = player;
#endif

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
#if USING_UNITY_GUI_SYSTEM

#else
        Transform relevantTransform;
        for (int teamIndex = 0; teamIndex < teams.Length; teamIndex++)
        {
            relevantTransform = teamIndex == 0 ? teamOneHeader : teamTwoHeader;
            for (int playerIndex = 0;  playerIndex < maxTeamSize; playerIndex++)
            {
                relevantTransform.GetChild(playerIndex).GetComponent<TextMeshProUGUI>().text = teams[teamIndex][playerIndex];
            }
        }
#endif
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
