using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using Unity.Netcode;
using Unity.Services.Authentication;



/// <summary>
/// Class that links all the player s classes for better communication between scripts and ease of access
/// </summary>
[DefaultExecutionOrder(-98)]
public sealed class PlayerFrame : NetworkBehaviour
{
    [field: SerializeField] public ChampionStats ChampionStats { get; set; }

    private PlayerCombat playerCombat;
    private WeaponHandler weaponHandler;

    private PlayerHealthNetworked playerHealth;

    private Controller.PlayerMovement playerMovement;
    private NetworkedPlayerPrimitive player;

    //private HandlePlayerNetworkBehaviour handlePlayerNetworkBehaviour;

    private bool WasInitiated => string.IsNullOrEmpty(playerName);
    private string playerName;



    public ushort PlayerID;
    public ushort TeamID => playerHealth.TeamID;

    public bool Alive => playerHealth.Alive;

    public void SetPlayerID(ushort playerID)
    {
        PlayerID = playerID;
    }

    public void InitPlayerFrameLocal(string playerName_)
    {
        InitPlayerCommon();

        playerCombat = GetComponent<PlayerCombat>();
        playerCombat.InitPlayerFrame(this);

        playerMovement = GetComponent<Controller.PlayerMovement>();
        playerMovement.InitPlayerFrame(this);

        ToggleCursor(false);

        playerName = playerName_;
        player = new(playerName, NetworkObjectId);

        _ = GetComponent<NetworkObject>() ?? throw new System.Exception("Does not have a network object");
        
        Game.Manager.RegisterPlayerServerRpc(player);
    }

    public void InitPlayerFrameRemote()
    {
        InitPlayerCommon();
    }

    private void InitPlayerCommon()
    {
        //playerCombat = GetComponent<PlayerCombat>();
        //playerCombat.InitPlayerFrame(this);

        weaponHandler = GetComponent<WeaponHandler>();
        weaponHandler.InitPlayerFrame(this);

        playerHealth = GetComponent<PlayerHealthNetworked>();
        playerHealth.InitPlayerFrame(this);

        //playerMovement = GetComponent<Controller.PlayerMovement>();
        //playerMovement.InitPlayerFrame(this);
    }

    public NetworkedPlayerPrimitive AsPrimitive(/*ulong requestingClientID*/)
    {
        //if (!WasInitiated)
        //{
        //    RequestPlayerNameServerRpc(NetworkManager.Singleton.LocalClientId, requestingClientID);
        //}

        return new(playerName, NetworkObjectId);
    }

    #region Toggle Controls

    public void ToggleGameControls(bool towardOn)
    {
        ToggleCameraInputs(towardOn);
        ToggleActionInputs(towardOn);
    }

    public void ToggleCameraInputs(bool towardOn)
    {
        transform.GetChild(0).GetComponent<Controller.FollowRotationCamera>().enabled = towardOn;
    }

    public void ToggleActionInputs(bool towardOn)
    {
        GetComponent<PlayerCombat>().enabled = towardOn;
        GetComponent<Controller.PlayerMovement>().enabled = towardOn;
    }

    public void ToggleCursor(bool towardOn)
    {
        Cursor.lockState = towardOn ? CursorLockMode.None : CursorLockMode.Locked; //
        Cursor.visible = !towardOn;
        // those two may cause issues when destroying the script on remote players // chekc when loggin concurrently
    }

    #endregion

    //[ServerRpc]
    //private void RequestPlayersDataServerRpc(ulong requestingClientID)
    //{
    //    foreach (var client in NetworkManager.Singleton.ConnectedClientsList)
    //    {
    //        if (client.ClientId != requestingClientID)
    //        {

    //            SendPlayerDataClientRpc(new(playerName, requestingClientID);
    //        }
    //    }
    //}

    [Rpc(SendTo.Server)]
    private void RequestPlayerNameServerRpc(ulong targetID, ulong requestingID)
    {
        foreach (var client in NetworkManager.Singleton.ConnectedClientsList)
        {
            if (client.ClientId == targetID)
            {
                SendPlayerNameClientRpc(new NetworkSerializableString(playerName), requestingID);
                return;
            }
        }
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void SendPlayerNameClientRpc(NetworkSerializableString playerName_, ulong targetClientID)
    {
        if (NetworkManager.Singleton.LocalClientId == targetClientID)
        {
            playerName = playerName_;
        }
    }

    //[Rpc(SendTo.ClientsAndHost)]
    //private void SendPlayerDataClientRpc(PlayerData data, ulong targetClientID)
    //{
    //    if (NetworkManager.Singleton.LocalClientId == targetClientID)
    //    {

    //    }
    //}
}

public struct PlayerData : INetworkSerializable
{
    public string PlayerName;
    public ulong ClientID;

    public PlayerData(string playerName, ulong clientID)
    {
        PlayerName = playerName;
        ClientID = clientID;
    }

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref PlayerName);
        serializer.SerializeValue(ref ClientID);
    }
}