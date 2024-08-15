using System;
using System.Collections;
using System.Collections.Generic;

using Unity.Netcode;
using UnityEngine;

using GameManagement;

[DefaultExecutionOrder(-97)]
[Serializable]
public sealed class HandlePlayerNetworkBehaviour : NetworkBehaviour, IPlayerFrameMember
{

    public PlayerFrame PlayerFrame { get; set; }

    public void InitPlayerFrame(PlayerFrame playerFrame)
    {
        PlayerFrame = playerFrame;
    }

    [Header("ToKillOnForeign")]
    [SerializeField] private List<Component> componentsToKillOnForeignPlayers;
    [SerializeField] private List<GameObject> gameObjectsToKillOnForeignPlayers;

    [Space(20)]
    [Header("ToKillOnSelf")]
    [SerializeField] private List<Component> componentsToKillOnLocalPlayers;
    [SerializeField] private List<GameObject> gameObjectsToKillOnLocalPlayers;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        //ManageFiles();
    }


    [Rpc(SendTo.Server)]
    public void ManageFilesAllServerRpc()
    {
        ManageFilesAllClientRpc();
    }

    [Rpc(SendTo.ClientsAndHost)]
    public void ManageFilesAllClientRpc()
    {
        var (componentsToKill, gameObjectsToKill) = IsOwner ? (componentsToKillOnLocalPlayers, gameObjectsToKillOnLocalPlayers) : (componentsToKillOnForeignPlayers, gameObjectsToKillOnForeignPlayers);

        foreach (var component in componentsToKill)
        {
            Destroy(component);
        }

        foreach (var gameObj in gameObjectsToKill)
        {
            Destroy(gameObj);
        }
    }

    public void ManageFiles()
    {
        var (componentsToKill, gameObjectsToKill) = IsOwner ? (componentsToKillOnLocalPlayers, gameObjectsToKillOnLocalPlayers) : (componentsToKillOnForeignPlayers, gameObjectsToKillOnForeignPlayers);

        foreach (var component in componentsToKill)
        {
            Destroy(component);
        }

        foreach (var gameObj in gameObjectsToKill)
        {
            Destroy(gameObj);
        }
    }

    public void ManageFiles(bool isOwner)
    {
        _ = isOwner ? ManageSelfFiles() : ManageForeignFiles();
    }

    public object ManageSelfFiles()
    {
        foreach (var component in componentsToKillOnLocalPlayers)
        {
            Destroy(component);
        }

        foreach (var gameObj in gameObjectsToKillOnLocalPlayers)
        {
            Destroy(gameObj);
        }

        return null;
    }

    public object ManageForeignFiles()
    {
        foreach (var component in componentsToKillOnForeignPlayers)
        {
            Destroy(component);
        }

        foreach (var gameObj in gameObjectsToKillOnForeignPlayers)
        {
            Destroy(gameObj);
        }

        return null;
    }

    public void ToggleGameControls(bool towardOn)
    {
        transform.GetChild(0).GetComponent<Controller.FollowRotationCamera>().enabled = towardOn;
        GetComponent<PlayerCombat>().enabled = towardOn;
        GetComponent<Controller.PlayerMovement>().enabled = towardOn;
    }

    public void ToggleCursor(bool towardOn)
    {
        Cursor.lockState = towardOn ? CursorLockMode.None : CursorLockMode.Locked; //
        Cursor.visible = !towardOn;
        // those two may cause issues when destroying the script on remote players // chekc when loggin concurrently
    }
}
