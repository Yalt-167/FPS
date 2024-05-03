using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
//using UnityEngine.Networking;

public class HandlePlayerNetworkBehaviour : NetworkBehaviour
{
    [SerializeField] private List<Component> componentsToKillOnForeignPlayers;
    [SerializeField] private List<GameObject> gameObjectsToKillOnForeignPlayers;

    #region Networking & Tears

    public override void OnNetworkSpawn()
    {
        if (!IsOwner)
        {
            foreach (var component in componentsToKillOnForeignPlayers)
            {
                Destroy(component);
            }

            foreach (var gameObj in gameObjectsToKillOnForeignPlayers)
            {
                Destroy(gameObj);
            }
        }
    }

    #endregion
}
