using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using System.Diagnostics;
using UnityEditor.PackageManager;
using System;



/// <summary>
/// Class that links all the player s classes for better communication between scripts and ease of access
/// </summary>
[DefaultExecutionOrder(-98)]
public class PlayerFrame : NetworkBehaviour
{
    //private NetworkLi
    [field: SerializeField] public ChampionStats ChampionStats { get; set; }

    private PlayerCombat playerCombat;
    private WeaponHandler weaponHandler;

    private PlayerHealthNetworked playerHealth;

    private PlayerMovement playerMovement;

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
        
        Game.Manager.RegisterPlayerServerRpc(new(playerName, NetworkObjectId));

        //Game.Manager.UpdatePlayerListServerRpc();

        //handlePlayerNetworkBehaviour.ManageFilesAllServerRpc();
    }


    public void InitPlayerFrameRemote()
    {
        playerCombat = GetComponent<PlayerCombat>();
        playerCombat.InitPlayerFrame(this);

        weaponHandler = GetComponent<WeaponHandler>();
        weaponHandler.InitPlayerFrame(this);

        playerHealth = GetComponent<PlayerHealthNetworked>();
        playerHealth.InitPlayerFrame(this);

        playerMovement = GetComponent<PlayerMovement>();
        playerMovement.InitPlayerFrame(this);

        //playerName = playerName_;
    }

    public NetworkedPlayerPrimitive AsPrimitive(/*ulong requestingClientID*/)
    {
        //if (!WasInitiated)
        //{
        //    RequestPlayerNameServerRpc(NetworkManager.Singleton.LocalClientId, requestingClientID);
        //}

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

    [ServerRpc]
    private void RequestPlayerNameServerRpc(ulong targetID, ulong requestingID)
    {
        foreach (var client in NetworkManager.Singleton.ConnectedClientsList)
        {
            if (client.ClientId == targetID)
            {
                SendPlayerNameClientRpc(new NetworkSerializableString(playerName), requestingID);
            }
        }
    }

    [ClientRpc]
    private void SendPlayerNameClientRpc(NetworkSerializableString playerName_, ulong targetClientID)
    {
        if (NetworkManager.Singleton.LocalClientId == targetClientID)
        {
            playerName = playerName_;
        }
    }

    //[ClientRpc]
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