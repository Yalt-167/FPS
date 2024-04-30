using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class HandlePlayerNetworkBehaviour : NetworkBehaviour
{
    [SerializeField] private List<Component> componentsToKillOnForeignPlayers;

    #region Networking & Tears

    public override void OnNetworkSpawn()
    {
        if (!IsOwner)
        {
            foreach (var component in componentsToKillOnForeignPlayers)
            {
                Destroy(component);
            }

        }
    }

    #endregion
}
