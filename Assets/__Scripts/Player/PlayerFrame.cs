using System.Collections;
using System.Collections.Generic;
using UnityEngine;



/// <summary>
/// Class that links all the player s classes for better communication between scripts and ease of access
/// </summary>
[DefaultExecutionOrder(-1001)]
public class PlayerFrame : MonoBehaviour
{
    private PlayerCombat playerCombat;
    private PlayerHealthNetworked playerHealth;
    private PlayerMovement playerMovement;
    private HandlePlayerNetworkBehaviour handlePlayerNetworkBehaviour;


    public ushort PlayerID;
    public ushort TeamID => playerHealth.TeamID;

    public bool Alive => playerHealth.Alive;

    public void SetPlayerID(ushort playerID)
    {
        PlayerID = playerID;
    }


    private void Awake()
    {
        print("was called");
        playerCombat = GetComponent<PlayerCombat>();
        playerHealth = GetComponent<PlayerHealthNetworked>();
        playerMovement = GetComponent<PlayerMovement>();
        handlePlayerNetworkBehaviour = GetComponent<HandlePlayerNetworkBehaviour>();
    }
}
