using System.Collections;
using System.Collections.Generic;
using UnityEngine;



/// <summary>
/// Class that links all the player s classes for better communication between scripts and ease of access
/// </summary>
[DefaultExecutionOrder(-98)]
public class PlayerFrame : MonoBehaviour
{
    [field: SerializeField] public ChampionStats ChampionStats { get; set; }
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
        playerCombat = GetComponent<PlayerCombat>();
        playerCombat.InitPlayerFrame(this);

        playerHealth = GetComponent<PlayerHealthNetworked>();
        playerHealth.InitPlayerFrame(this);

        playerMovement = GetComponent<PlayerMovement>();
        playerMovement.InitPlayerFrame(this);

        handlePlayerNetworkBehaviour = GetComponent<HandlePlayerNetworkBehaviour>();
        handlePlayerNetworkBehaviour.InitPlayerFrame(this);
    }
}
