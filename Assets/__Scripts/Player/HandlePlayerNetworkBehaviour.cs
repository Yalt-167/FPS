using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

[DefaultExecutionOrder(-97)]
[Serializable]
public class HandlePlayerNetworkBehaviour : NetworkBehaviour
{

    private PlayerFrame playerFrame;

    [Header("ToKillOnForeign")]
    [SerializeField] private List<Component> componentsToKillOnForeignPlayers;
    [SerializeField] private List<GameObject> gameObjectsToKillOnForeignPlayers;

    [Space(20)]
    [Header("ToKillOnSelf")]
    [SerializeField] private List<Component> componentsToKillOnLocalPlayers;
    [SerializeField] private List<GameObject> gameObjectsToKillOnLocalPlayers;

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

        Game.Manager.AddNetworkedWeaponHandler(GetComponent<WeaponHandler>());
    }

    #region Networking & Tears

    public override void OnNetworkSpawn()
    {
        transform.position = new(transform.position.x, 2, transform.position.z);


        ToggleControls(false);
        ManageFiles();
    }


    public override void OnNetworkDespawn()
    {

    }


    public void ToggleControls(bool towardOn)
    {
        transform.GetChild(0).GetComponent<FollowRotationCamera>().enabled = towardOn;
        GetComponent<PlayerCombat>().enabled = towardOn;
        GetComponent<PlayerMovement>().enabled = towardOn;
    }

    #endregion
}
