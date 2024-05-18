using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

[DefaultExecutionOrder(-1000)]
public class HandlePlayerNetworkBehaviour : NetworkBehaviour
{
    [Header("ToKillOnForeign")]
    [SerializeField] private List<Component> componentsToKillOnForeignPlayers;
    [SerializeField] private List<GameObject> gameObjectsToKillOnForeignPlayers;

    [Space(20)]
    [Header("ToKillOnSelf")]
    [SerializeField] private List<Component> componentsToKillOnLocalPlayers;
    [SerializeField] private List<GameObject> gameObjectsToKillOnLocalPlayers;

    #region Networking & Tears

    public override void OnNetworkSpawn() // also fix spawn in ground
    {
        transform.position = new(transform.position.x, 2, transform.position.z);

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

    #endregion
}
