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

    //private HandlePlayerNetworkBehaviour handlePlayerNetworkBehaviour;

    private string playerName;


    public ushort PlayerID;
    public ushort TeamID => playerHealth.TeamID;

    public bool Alive => playerHealth.Alive;

    public void SetPlayerID(ushort playerID)
    {
        PlayerID = playerID;
    }

    public void InitPlayerFrame(string playerName_)
    {
        playerCombat = GetComponent<PlayerCombat>();
        playerCombat.InitPlayerFrame(this);

        weaponHandler = GetComponent<WeaponHandler>();
        weaponHandler.InitPlayerFrame(this);

        playerHealth = GetComponent<PlayerHealthNetworked>();
        playerHealth.InitPlayerFrame(this);

        playerMovement = GetComponent<PlayerMovement>();
        playerMovement.InitPlayerFrame(this);

        //handlePlayerNetworkBehaviour = GetComponent<HandlePlayerNetworkBehaviour>();
        //handlePlayerNetworkBehaviour.InitPlayerFrame(this);
        //handlePlayerNetworkBehaviour.ToggleCursor(false);
        ToggleCursor(false);

        playerName = playerName_;

        var hasNetworkObject = GetComponent<NetworkObject>() ?? throw new System.Exception("Does not have");
        print($"NetworkObjectId: {NetworkObjectId}");
        Game.Manager.RegisterPlayerServerRpc(new(playerName, NetworkObjectId));

        Game.Manager.UpdatePlayerListServerRpc();

        //handlePlayerNetworkBehaviour.ManageFilesAllServerRpc();
    }

    public NetworkedPlayerPrimitive AsPrimitive()
    {
        return new(playerName, NetworkObjectId);
    }

    public void ToggleGameControls(bool towardOn)
    {
        ToggleCameraInputs(towardOn);
        ToggleActionInputs(towardOn);
    }

    public void ToggleCameraInputs(bool towardOn)
    {
        transform.GetChild(0).GetComponent<FollowRotationCamera>().enabled = towardOn;
    }

    public void ToggleActionInputs(bool towardOn)
    {
        GetComponent<PlayerCombat>().enabled = towardOn;
        GetComponent<PlayerMovement>().enabled = towardOn;
    }

    public void ToggleCursor(bool towardOn)
    {
        Cursor.lockState = towardOn ? CursorLockMode.None : CursorLockMode.Locked; //
        Cursor.visible = !towardOn;
        // those two may cause issues when destroying the script on remote players // chekc when loggin concurrently
    }

}
