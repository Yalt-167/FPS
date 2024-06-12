using System.Collections;
using System.Collections.Generic;
using Unity.Multiplayer.Samples.Utilities.ClientAuthority;
using Unity.Netcode;
using UnityEngine;

[DefaultExecutionOrder(-1000)]
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



    #region Networking & Tears

    public override void OnNetworkSpawn()
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

        GetComponent<PlayerFrame>().SetPlayerID(
            Game.Manager.RegisterPlayer(
                new(
                    GetComponent<NetworkObject>(),
                    GetComponent<ClientNetworkTransform>(),
                    GetComponent<HandlePlayerNetworkBehaviour>(),
                    GetComponent<WeaponHandler>(),
                    GetComponent<PlayerHealthNetworked>()
                )
            )
        );
    }


    public override void OnNetworkDespawn()
    {
        Game.Manager.DiscardNetworkedWeaponHandler(GetComponent<WeaponHandler>());

        Game.Manager.DiscardPlayer(GetComponent<PlayerFrame>().PlayerID);
    }

    #endregion
}
