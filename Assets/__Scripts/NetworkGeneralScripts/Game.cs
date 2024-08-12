//#define DEBUG_MULTIPLAYER
#define LOG_METHOD_CALLS

using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Multiplayer.Samples.Utilities.ClientAuthority;
using Unity.Netcode;
using UnityEngine;
using System.Linq;
using Random = UnityEngine.Random;
using UnityEditor;
using System.Text;
using static DebugUtility;


[DefaultExecutionOrder(-99)]
public sealed class Game : NetworkManager
{
    public static Game Manager;

    #region Player List

    public List<NetworkedPlayer> players = new();

    [Rpc(SendTo.Server)]
    public void RegisterPlayerServerRpc(NetworkedPlayerPrimitive player)
    {
#if LOG_METHOD_CALLS
        LogMethodCall();
#endif
        RegisterPlayerInternalClientRpc(player);
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void RegisterPlayerInternalClientRpc(NetworkedPlayerPrimitive player)
    {

#if DEBUG_MULTIPLAYER
        print("[Debug Multiplayer] Added a player");
#endif
        players.Add(player.AsNetworkedPlayer());
    }


    public void DisconnectPlayer(ushort playerID)
    {
        var player = players[playerID];
        player.Online = false;
        players[playerID] = player;
    }

    /// <summary>
    /// <paramref name="whichComponentID"/> basically refers to which component ID was passed in the function.<br/>
    /// For instance if the ID we have is the weaponHandler ID and we passed it we should also pass the relevant enum member 
    /// </summary>
    /// <param name="componentID"></param>
    /// <param name="whichComponentID"></param>
    public NetworkedPlayer RetrievePlayerFromComponentID(ulong componentID, NetworkedComponent whichComponentID)
    {
        return whichComponentID switch
        {
            NetworkedComponent.NetworkObject => players.First(predicate: (each) => each.NetworkObject.NetworkObjectId == componentID),

            NetworkedComponent.ClientNetworkTransform => players.First(predicate: (each) => each.ClientNetworkTransform.NetworkObjectId == componentID),

            //NetworkedComponent.HandlePlayerNetworkBehaviour => players.First(each => each.BehaviourHandler.NetworkObjectId == componentID),

            NetworkedComponent.WeaponHandler => players.First(predicate: (each) => each.WeaponHandler.NetworkObjectId == componentID),

            NetworkedComponent.PlayerHealthNetworked => players.First(predicate: (each) => each.Health.NetworkObjectId == componentID),

            _ => throw new Exception($"This component provided ({whichComponentID}) does not match anything"),
        };
        
    }

    public NetworkedPlayer RetrievePlayerFromAbsoluteID(ushort ID)
    {
        return players[ID];
    }

    public IEnumerable<NetworkedPlayer> GetPlayers()
    {
        foreach (var player in players)
        {
            yield return player;
        }
    }
    
    public IEnumerable<NetworkedPlayer> GetPlayersOfTeam(ushort teamID)
    {
        foreach (var player in players)
        {
            if (player.TeamID == teamID)
            {
                yield return player;
            }
        }
    }

    public bool PlayerWithNameExist(string name)
    {
        return GetPlayerWithName(name) != null;
    }

    public NetworkedPlayer? GetPlayerWithName(string name)
    {
        try
        {
            return players.First(predicate: (each) => each.Name == name);
        }
        catch
        {
            return null;
        }

    }

    [MenuItem("Developer/DebugPlayerList")]
    public static void DebugPlayerList()
    {
        //PrintIterable(Manager.players);
        var stringBuilder = new StringBuilder();

        stringBuilder.Append("[ ");
        var isFirst = true;
        foreach (var player in Manager.players)
        {
            stringBuilder.Append(isFirst ? $"{player.GetInfos()}" : $", {player.GetInfos()}");
            isFirst = false;
        }
        stringBuilder.Append(" ]");

        Debug.Log(stringBuilder.ToString());
    }

    public static void PrintIterable(IEnumerable iterable)
    {
        var stringBuilder = new StringBuilder();

        stringBuilder.Append("[ ");
        var isFirst = true;
        foreach (var item in iterable)
        {
            stringBuilder.Append(isFirst ? $"{item.ToString()}" : $", {item.ToString()}");
            isFirst = false;
        }
        stringBuilder.Append(" ]");

        Debug.Log(stringBuilder.ToString());
    }


#endregion

    #region Player Combat List

    private readonly List<WeaponHandler> networkedWeaponHandlers = new();

    public void AddNetworkedWeaponHandler(WeaponHandler networkedWeaponHandler)
    {
        networkedWeaponHandlers.Add(networkedWeaponHandler);
    }

    public void DiscardNetworkedWeaponHandler(WeaponHandler networkedWeaponHandler)
    {
        networkedWeaponHandlers.Remove(networkedWeaponHandler);
    }

    public WeaponHandler GetNetworkedWeaponHandlerFromNetworkObjectID(ulong playerObjectID) // ? make it so they are sorted ? -> slower upon adding it but faster to select it still
    {
        foreach (var networkedWeaponHandler in networkedWeaponHandlers)
        {
            if (networkedWeaponHandler.NetworkObjectId == playerObjectID)
            {
                return networkedWeaponHandler;
            }
        }

        return null;
    }

    public IEnumerable<WeaponHandler> GetNetworkedWeaponHandlers()
    {
        foreach (var networkHandler in networkedWeaponHandlers)
        {
            yield return networkHandler;
        }
    }


    #endregion

    #region Network Objects Spawning

    [Rpc(SendTo.Server)]
    public void RequestNetworkObjectClientSpawnServerRpc(GameObject networkObjectPrefab, Vector3 position, Quaternion orientation)
    {
        if (networkObjectPrefab.TryGetComponent<NetworkObject>(out var _))
        {
            SpawnNetworkObjectClientRpc(networkObjectPrefab, position, orientation);
        }
        else
        {
            Debug.LogError("NetworkPrefab is missing a NetworkObject component.");
        }
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void SpawnNetworkObjectClientRpc(GameObject networkObjectPrefab, Vector3 position, Quaternion orientation)
    {
        // Actually spawn the obj on the network
        Instantiate(networkObjectPrefab, position, orientation).GetComponent<NetworkObject>().Spawn();
    }

    #endregion

    #region Network Objects Monitoring

    [MenuItem("Developer/DebugNetworkObjects")]
    public static void DebugNetworkObjects()
    {
        foreach (KeyValuePair<ulong, NetworkObject> element in NetworkManager.Singleton.SpawnManager.SpawnedObjects)
        {
            print($"NetworkObjectId: {element.Key} | Obj: {element.Value.name}");
        }
    }

    [MenuItem("Developer/DebugNetworkBehaviours")]
    public static void DebugNetworkBehaviours()
    {
        foreach (KeyValuePair<ulong, NetworkObject> element in NetworkManager.Singleton.SpawnManager.SpawnedObjects)
        {
            print($"NetworkObjectId: {element.Key} | Obj: {element.Value.name}");
            foreach (NetworkBehaviour networkBehaviour in element.Value.GetComponents<NetworkBehaviour>())
            {
                print($"Behaviour: {networkBehaviour.GetType()} | NetworkBehaviourId: {networkBehaviour.NetworkBehaviourId}");
            }
        }
    }

    #endregion

    [ServerRpc]
    public void UpdatePlayerListServerRpc(ServerRpcParams rpcParams = default)
    {
        UpdatePlayerListClientRpc(GetPlayersAsPrimitives(/*rpcParams.Receive.SenderClientId*/));
    }

    private NetworkedPlayerPrimitive[] GetPlayersAsPrimitives(/*ulong requestingClientID*/)
    {
        NetworkedPlayerPrimitive[] asPrimitives = new NetworkedPlayerPrimitive[NetworkManager.Singleton.SpawnManager.SpawnedObjects.Count];

#if DEBUG_MULTIPLAYER
        var stringBuilder = new StringBuilder();
        stringBuilder.Append("[ ");
#endif
        var idx = 0;
        foreach (KeyValuePair<ulong, NetworkObject> element in NetworkManager.Singleton.SpawnManager.SpawnedObjects)
        {
#if DEBUG_MULTIPLAYER
            stringBuilder.Append($"{{{element.Key}, {element.Value}}} ");
#endif
            if (element.Value.TryGetComponent<PlayerFrame>(out var playerFrameComponent))
            {
                asPrimitives[idx
#if DEBUG_MULTIPLAYER
                    ++
#endif
                    ] = playerFrameComponent.AsPrimitive(/*requestingClientID*/);
            }
        }
#if DEBUG_MULTIPLAYER
        stringBuilder.Append("]");
        print(stringBuilder.ToString());
#endif
        return asPrimitives;
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void UpdatePlayerListClientRpc(NetworkedPlayerPrimitive[] playerPrimitives)
    {
#if DEBUG_MULTIPLAYER
        print("[ClientRpc] UpdatePlayerList");
#endif
        playerPrimitives = GetPlayersAsPrimitives();
        players.Clear();
        for (int i = 0; i < playerPrimitives.Length; i++)
        {
            //print(playerPrimitives[i].ObjectNetworkID);
            players.Add(playerPrimitives[i].AsNetworkedPlayer());
        }
    }


    [Rpc(SendTo.Server)]
    public void RetrieveExistingPlayerListServerRpc()
    {
        if (!IsServer) { return; }

#if LOG_METHOD_CALLS
        LogMethodCall();
#endif

        SetPlayerListClientRpc(players);
    }

    [ClientRpc]
    private void SetPlayerListClientRpc(List<NetworkedPlayer> players_)
    {
#if LOG_METHOD_CALLS
        LogMethodCall();
#endif
        
        players = players_;
#if DEBUG_MULTIPLAYER
        print($"[Debug Multiplayer] Count: {players_.Count}");
        foreach (var player in players_)
        {
            print(player.Name);
        }

        foreach (var player in players)
        {
            print(player.Name);
        }
#endif
    }




    #region Respawn Logic

    private readonly Dictionary<ushort, List<SpawnPoint>> spawnPoints = new();

    public void AddRespawnPoint(SpawnPoint spawnPoint)
    {
        if (!spawnPoints.ContainsKey(spawnPoint.TeamID))
        {
            spawnPoints.Add(spawnPoint.TeamID, new());
        }

        spawnPoints[spawnPoint.TeamID].Add(spawnPoint);
    }


    public void DiscardRespawnPoint(SpawnPoint spawnPoint)
    {
        spawnPoints[spawnPoint.TeamID].Remove(spawnPoint);
    }

    public Vector3 GetSpawnPosition(ushort teamID)
    {
        var spawnPointExists = spawnPoints.TryGetValue(teamID, out var relevantSpawnPoints);

        if (!spawnPointExists) { return NoSpawnpointAvailableForThisTeam(teamID); }

        // filtering the active ones
        var activeRelevantSpawnPoints = (List<SpawnPoint>)relevantSpawnPoints.Where(predicate: (spawnPoint) => spawnPoint.Active);

        if (activeRelevantSpawnPoints.Count == 0) { return NoSpawnpointAvailableForThisTeam(teamID); }

        return activeRelevantSpawnPoints[Random.Range(0, activeRelevantSpawnPoints.Count - 1)].SpawnPosition;
    }

    private Vector3 NoSpawnpointAvailableForThisTeam(ushort teamID)
    {
        print($"There s no checkpoint available for this player with team ID: {teamID}");
        return Vector3.zero;
    }

    #endregion

    private void Awake()
    {
        Manager = this;
    }


    [ServerRpc]
    private void UpdateGameManagerServerRpc()
    {
#if DEBUG_MULTIPLAYER
        print("ServerRpc");
#endif
        UpdateGameManagerClientRpc(Manager);
    }

    [ClientRpc]
    private void UpdateGameManagerClientRpc(Game manager)
    {
#if DEBUG_MULTIPLAYER
        print("ClientRpc");
#endif

        Manager = manager;
    }

    #region Debug

    //[SerializeField] private bool debugBottomPlane;

    //private void OnDrawGizmos()
    //{
    //    if (debugBottomPlane)
    //    {
    //        Gizmos.color = Color.red;
    //        var origin = new Vector3(transform.position.x, -30, transform.position.z);
    //        var debugDist = 100f; // how far the plane with be rendered

    //        var sideward = Vector3.right * debugDist;
    //        var forward = Vector3.forward * debugDist;
    //        for (int offset = -100; offset <= 100; offset += 10)
    //        {
    //            var forwardOffsetVec = new Vector3(0, 0, offset);
    //            Gizmos.DrawLine(origin - sideward + forwardOffsetVec, origin + sideward + forwardOffsetVec);

    //            var sidewardOffsetVec = new Vector3(offset, 0, 0);
    //            Gizmos.DrawLine(origin - forward + sidewardOffsetVec, origin + forward + sidewardOffsetVec);
    //        }
    //    }
    //}

    #endregion

}

[Serializable]
public struct Settings
{
    public bool viewBobbing;
}



[Serializable]
public struct NetworkedPlayer
{
    public string Name;
    public ushort TeamID;
    public NetworkObject NetworkObject;
    public ClientNetworkTransform ClientNetworkTransform;
    //public HandlePlayerNetworkBehaviour BehaviourHandler;
    public WeaponHandler WeaponHandler;
    public PlayerHealthNetworked Health;
    public bool Online;

    public NetworkedPlayer(
        string name,
        ushort teamID,
        NetworkObject object_,
        ClientNetworkTransform transform_,
        //HandlePlayerNetworkBehaviour behaviourHandler,
        WeaponHandler weaponHandler,
        PlayerHealthNetworked health
    )
    {
        Name = name;
        TeamID = teamID;
        NetworkObject = object_;
        ClientNetworkTransform = transform_;
        //BehaviourHandler = behaviourHandler;
        WeaponHandler = weaponHandler;
        Health = health;
        Online = true;
    }

    public readonly string GetInfos()
    {
        return $"{{Player: {Name} | Team: {TeamID}}}";
    }

    public new readonly string ToString()
    {
        return GetInfos();
    }

    public readonly NetworkedPlayerPrimitive AsNetworkedPlayerPrimitive()
    {
        return new NetworkedPlayerPrimitive(Name, NetworkObject.NetworkObjectId);
    }
}

[Serializable]
public struct NetworkedPlayerPrimitive : INetworkSerializable
{
    public string Name;
    public ulong ObjectNetworkID;

    public NetworkedPlayerPrimitive(string name, ulong objectNetworkID)
    {
        Name = name;
        ObjectNetworkID = objectNetworkID;
#if DEBUG_MULTIPLAYER
        Debug.Log($"Name: {Name} | objectNetworkID: {ObjectNetworkID}");
#endif
    }

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref Name);
        serializer.SerializeValue(ref ObjectNetworkID);
    }

    public readonly NetworkedPlayer AsNetworkedPlayer() // findAnchor
    {
#if DEBUG_MULTIPLAYER
        Debug.Log($"Name: {Name} | objectNetworkID: {ObjectNetworkID}");
#endif
        if (!NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(ObjectNetworkID, out var networkObject))
        {
            throw new System.Exception($"This player (ObjectNetworkID: {ObjectNetworkID}) was not properly spawned");
        }

        return new NetworkedPlayer(
                Name,
                0,
                networkObject,
                networkObject.GetComponent<ClientNetworkTransform>(),
                //networkObject.GetComponent<HandlePlayerNetworkBehaviour>(),
                networkObject.GetComponent<WeaponHandler>(),
                networkObject.GetComponent<PlayerHealthNetworked>()
            );
    }

}

public enum NetworkedComponent : byte
{
    NetworkObject,
    ClientNetworkTransform,
    //HandlePlayerNetworkBehaviour,
    WeaponHandler,
    PlayerHealthNetworked
}
