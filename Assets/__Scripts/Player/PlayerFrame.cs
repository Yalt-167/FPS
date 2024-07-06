using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;



/// <summary>
/// Class that links all the player s classes for better communication between scripts and ease of access
/// </summary>
[DefaultExecutionOrder(-98)]
public class PlayerFrame : NetworkBehaviour
{
    [field: SerializeField] public ChampionStats ChampionStats { get; set; }

    private PlayerCombat playerCombat;
    private WeaponHandler weaponHandler;

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

    public void InitPlayerFrame(string playerName)
    {
        playerCombat = GetComponent<PlayerCombat>();
        playerCombat.InitPlayerFrame(this);

        weaponHandler = GetComponent<WeaponHandler>();
        weaponHandler.InitPlayerFrame(this);

        playerHealth = GetComponent<PlayerHealthNetworked>();
        playerHealth.InitPlayerFrame(this);

        playerMovement = GetComponent<PlayerMovement>();
        playerMovement.InitPlayerFrame(this);

        handlePlayerNetworkBehaviour = GetComponent<HandlePlayerNetworkBehaviour>();
        handlePlayerNetworkBehaviour.InitPlayerFrame(this);
        handlePlayerNetworkBehaviour.ToggleCursor(false);

        var hasNetworkObject = GetComponent<NetworkObject>() ?? throw new System.Exception("Does not have");

        Game.Manager.RegisterPlayerServerRpc(
            new(playerName, GetComponent<NetworkObject>().NetworkObjectId)
            );

        Game.Manager.UpdatePlayerListServerRpc();

        //handlePlayerNetworkBehaviour.ManageFilesAllServerRpc();
    }
}
