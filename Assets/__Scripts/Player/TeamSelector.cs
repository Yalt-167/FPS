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
    public static TeamSelector Instance { get; private set; }
    private string[][] teams;
    [SerializeField] private ushort teamsCount;
    [SerializeField] private ushort maxTeamSize;
    [SerializeField] private int teamSelectionMenuWidth;
    [SerializeField] private int teamSelectionMenuPlayerRegionHeightPerExpectedPlayer;
    private int[] teamsIndex;

    private bool teamSelectorMenuActive;


#if !USING_UNITY_GUI_SYSTEM
    private Transform teamOneHeader;
    private Transform teamTwoHeader;
    private Button startGameButton;
#endif

    private void Awake()
    {
        Instance = this;
        ToggleSelectionScreenMenu(towardOn: true);
#if !USING_UNITY_GUI_SYSTEM
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
        teamSelectorMenuActive = true;
        teams = new string[teamsCount][];
        teamsIndex = new int[teamsCount];
        for (int i = 0; i < teamsCount; i++)
        {
            teams[i] = new string[maxTeamSize];
            teamsIndex[i] = 0;
        }
    }

    private void Update()
    {
#if !USING_UNITY_GUI_SYSTEM

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
        GUILayout.BeginVertical("box"); // V

            GUILayout.Label($"Team{teamNumber}");

            GUILayout.BeginHorizontal("box", GUILayout.Width(teamSelectionMenuWidth)); // VH

                if (GUILayout.Button("Join team"))
                {
                    OnTeamSelected((ushort)teamNumber);
                }

            GUILayout.EndHorizontal(); // V

            GUILayout.BeginVertical("box", GUILayout.Height(teamSelectionMenuPlayerRegionHeightPerExpectedPlayer * maxTeamSize)); // VV

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
            GUILayout.BeginVertical("box");

                CreateTeamMenu(i + 1);

            GUILayout.EndVertical();

            GUILayout.FlexibleSpace();
        }

        GUILayout.EndHorizontal();

        GUILayout.FlexibleSpace();

        if (LobbyHandling.LobbyHandler.Instance.IsLobbyHost())
        {
            GUILayout.BeginHorizontal(GUILayout.Width(Screen.width));

            GUILayout.FlexibleSpace();

            if (GUILayout.Button("Start game"))
            {
                DisableTeamSelectionScreenServerRpc();
            }

            GUILayout.FlexibleSpace();

            GUILayout.EndHorizontal();
        }

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
#if !USING_UNITY_GUI_SYSTEM
        (teamIndex == 0 ? teamOneHeader : teamTwoHeader).GetChild(teamsIndex[teamIndex]).GetComponent<TextMeshProUGUI>().text = player;
#endif

        teamsIndex[teamIndex]++;

        GameNetworkManager.Manager.GetPlayerFromName(player).SetTeam((ushort)(teamIndex + 1));
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
#if !USING_UNITY_GUI_SYSTEM
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
        ToggleSelectionScreenMenu(towardOn: false);

        PlayerFrame.LocalPlayer.ToggleCursor(towardOn: false);

        PlayerFrame.LocalPlayer.ToggleGameControls(towardOn: true);
        PlayerFrame.LocalPlayer.ToggleCamera(towardOn: true);
        PlayerFrame.LocalPlayer.ToggleHUD(towardOn: true);
    }

    private void ToggleSelectionScreenMenu(bool towardOn)
    {
        teamSelectorMenuActive = towardOn;
    }
}
