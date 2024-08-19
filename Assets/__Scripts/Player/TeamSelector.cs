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

    private PlayerFrame playerFrame;


    private Transform wholeTeamSelectionMenu;
    private Transform teamOneHeader;
    private Transform teamTwoHeader;
    private Button startGameButton;

    private readonly int startGameButtonWith = 160;
    private readonly int startGameButtonHeight = 30;

    private Rect startGameButtonRect;

    private float screenRatio;
    private void Awake()
    {
        active = true;
        teams = new string[2][] { new string[maxTeamSize], new string[maxTeamSize] };

        playerFrame = GetComponent<PlayerFrame>();

        wholeTeamSelectionMenu = transform.GetChild(5);

        teamOneHeader = wholeTeamSelectionMenu.GetChild(1);
        teamOneHeader.GetComponent<Button>().onClick.AddListener(JoinTeamOne);

        teamTwoHeader = wholeTeamSelectionMenu.GetChild(2);
        teamTwoHeader.GetComponent<Button>().onClick.AddListener(JoinTeamTwo);


        startGameButton = wholeTeamSelectionMenu.GetChild(3).GetComponent<Button>();
        startGameButtonRect = new(Screen.width / 2 - startGameButtonWith / 2, 133 + startGameButtonHeight / 2, startGameButtonWith, startGameButtonHeight);
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        SetRelevantPlayerFrame();
    }

    private void OnGUI()
    {
        if (!IsOwner) { return; }

        playerFrame.ToggleCursor(towardOn: active);
        playerFrame.ToggleGameControls(towardOn: !active);

        if (!active) { return; }

        if (Input.GetMouseButtonDown(0))
        {
            OnTeamSelected((ushort)(Input.mousePosition.x < Screen.width / 2 ? 1 : 2));
        }

        if (!IsHost) { return; }

        if (Input.GetKeyDown(KeyCode.Return))
        {
            Game.StaticStartGame();
            DisableTeamSelectionScreenServerRpc();
        }
    }


    public void JoinTeamOne()
    {
        Debug.Log("Join Team 1 was clicked");
        OnTeamSelected(1);
    }

    public void JoinTeamTwo()
    {
        Debug.Log("Join Team 2 was clicked");
        OnTeamSelected(2);
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
            RemovePlayerFromTeamList(otherTeamIdx, player);
        }

        teams[teamIndex][teamsIndex[teamIndex]] = player;

        (teamIndex == 0 ? teamOneHeader : teamTwoHeader).GetChild(teamsIndex[teamIndex]).GetComponent<TextMeshProUGUI>().text = player;

        teamsIndex[teamIndex]++;

        //active = false;
        //wholeTeamSelectionMenu.gameObject.SetActive(false);
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
        Transform relevantTransform = null;
        for (int teamIndex = 0; teamIndex < teams.Length; teamIndex++)
        {
            relevantTransform = teamIndex == 0 ? teamOneHeader : teamTwoHeader;
            for (int playerIndex = 0;  playerIndex < maxTeamSize; playerIndex++)
            {
                relevantTransform.GetChild(playerIndex).GetComponent<TextMeshProUGUI>().text = teams[teamIndex][playerIndex];
            }
        }
    }

    private bool IsOnStartGameButton()
    {
        return IsHost && startGameButtonRect.Contains(Input.mousePosition);
    }

    private IEnumerator CycleRandomActions()
    {
        for (; ; )
        {
            //OnTeamSelected((ushort)UnityEngine.Random.Range(0f, 1f));
            (UnityEngine.Random.Range(0f, 1f) < .5f ? teamOneHeader : teamTwoHeader).GetComponent<Button>().onClick.Invoke();
            yield return new WaitForSeconds(1f);
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
        wholeTeamSelectionMenu.gameObject.SetActive(false);
        playerFrame.ToggleCursor(towardOn: false);
        playerFrame.ToggleGameControls(towardOn: true);
    }

    [Rpc(SendTo.Server)]
    private void SetRelevantPlayerFrameServerRpc()
    {
        SetRelevantPlayerFrameClientRpc();
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void SetRelevantPlayerFrameClientRpc()
    {
        foreach (var player in GameNetworkManager.Manager.Players)
        {
            if (player.IsOwner)
            {
                playerFrame = player;
            }
        }
    }

    private void SetRelevantPlayerFrame()
    {
        foreach (var player in GameNetworkManager.Manager.Players)
        {
            if (player.IsOwner)
            {
                playerFrame = player;
            }
        }
    }
}
