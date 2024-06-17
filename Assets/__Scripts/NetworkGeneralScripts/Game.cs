using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Multiplayer.Samples.Utilities.ClientAuthority;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Linq;
using Random = UnityEngine.Random;
using UnityEditor;

[DefaultExecutionOrder(-1002)]
public sealed class Game : NetworkBehaviour
{
    public static Game Manager;

    #region Debug

    [SerializeField] private bool debugBottomPlane;

    #endregion

    #region Player List

    private readonly NetworkedPlayer NO_PLAYER = new();
    public readonly List<NetworkedPlayer> players = new();

    /// <summary>
    ///  returns ur absolute ID (fancy word for index in playerList)<br/>
    ///  this ID ensure faster retrieval
    /// </summary>
    /// <param name="player"></param>
    /// <returns></returns>
    [Rpc(SendTo.Server)]
    public void RegisterPlayerServerRpc(NetworkedPlayerPrimitive player)
    {
        RegisterPlayerInternalClientRpc(player);
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void RegisterPlayerInternalClientRpc(NetworkedPlayerPrimitive player)
    {
        players.Add(player.ToNetworkedPlayer());
    }


    public void DiscardPlayer(ushort playerID)
    {
        players[playerID] = NO_PLAYER;
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
            NetworkedComponent.NetworkObject => players.First(each => ((NetworkObject)each).NetworkObjectId == componentID),

            NetworkedComponent.ClientNetworkTransform => players.First(each => ((ClientNetworkTransform)each).NetworkObjectId == componentID),

            NetworkedComponent.HandlePlayerNetworkBehaviour => players.First(each => ((HandlePlayerNetworkBehaviour)each).NetworkObjectId == componentID),

            NetworkedComponent.WeaponHandler => players.First(each => ((WeaponHandler)each).NetworkObjectId == componentID),

            NetworkedComponent.PlayerHealthNetworked => players.First(each => ((PlayerHealthNetworked)each).NetworkObjectId == componentID),

            _ => throw new Exception("This component provided does not match anything"),
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
            return players.First(each => each.Name == name);
        }
        catch
        {
            return null;
        }

    }

    [MenuItem("Developper/DebugPlayerList")]
    public static void DebugPlayerList()
    {
        foreach (var player in Manager.players)
        {
            Debug.Log(player.GetInfos());
        }
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
        foreach (var networkHandler in networkedWeaponHandlers)
        {
            if (networkHandler.NetworkObjectId == playerObjectID)
            {
                return networkHandler;
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

    #region Respawn Logic

    private Dictionary<ushort, List<SpawnPoint>> spawnPoints = new Dictionary<ushort, List<SpawnPoint>>();


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
        var relevantSpawnPoints = spawnPoints[teamID];
        var relevantSpawnPointsCount = relevantSpawnPoints.Count;

        // filtering the active ones
        var activeRelevantSpawnPoints = new List<SpawnPoint>();
        for (int i = 0; i < relevantSpawnPointsCount; i++)
        {
            if (relevantSpawnPoints[i].Active)
            {
                activeRelevantSpawnPoints.Add(relevantSpawnPoints[i]);
            }
        }

        var activeRelevantSpawnPointsCount = activeRelevantSpawnPoints.Count;
        if (activeRelevantSpawnPointsCount == 0) { throw new Exception($"There s no checkpoint available for this player with team ID: {teamID}"); }

        return activeRelevantSpawnPoints[Random.Range(0, activeRelevantSpawnPointsCount - 1)].SpawnPosition;
    }

    #endregion

    [field: SerializeField] public Settings GameSettings { get; private set; }

    public int CurrentSceneID { get; private set; } = 1;

    private void Awake()
    {
        Manager = this;
    }


    private void Start()
    {
        Application.targetFrameRate = 60;
    }

    
    public void NextLevel()
    {
        SceneManager.LoadScene(++CurrentSceneID);
    }

    private void OnDrawGizmos()
    {
        if (debugBottomPlane)
        {
            Gizmos.color = Color.red;
            var origin = new Vector3(transform.position.x, -30, transform.position.z);
            var debugDist = 100f; // how far the plane with be rendered

            var sideward = Vector3.right * debugDist;
            var forward = Vector3.forward * debugDist;
            for (int offset = -100; offset <= 100; offset += 10)
            {
                var forwardOffsetVec = new Vector3(0, 0, offset);
                Gizmos.DrawLine(origin - sideward + forwardOffsetVec, origin + sideward + forwardOffsetVec);

                var sidewardOffsetVec = new Vector3(offset, 0, 0);
                Gizmos.DrawLine(origin - forward + sidewardOffsetVec, origin + forward + sidewardOffsetVec);
            }
        }
    }
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
    public NetworkObject Object;
    public ClientNetworkTransform Transform;
    public HandlePlayerNetworkBehaviour BehaviourHandler;
    public WeaponHandler WeaponHandler;
    public PlayerHealthNetworked Health;

    public NetworkedPlayer(
        string name,
        ushort teamID,
        NetworkObject object_,
        ClientNetworkTransform transform_,
        HandlePlayerNetworkBehaviour behaviourHandler,
        WeaponHandler weaponHandler,
        PlayerHealthNetworked health
    )
    {
        Name = name;
        TeamID = teamID;
        Object = object_;
        Transform = transform_;
        BehaviourHandler = behaviourHandler;
        WeaponHandler = weaponHandler;
        Health = health;
    }

    public string GetInfos()
    {
        return $"Player: {Name} / Team: {TeamID}";
    }


    #region QoL

    #region Practical Getters

    public static explicit operator string(NetworkedPlayer relevantPlayer) => relevantPlayer.Name;
    public static explicit operator ushort(NetworkedPlayer relevantPlayer) => relevantPlayer.TeamID;
    public static explicit operator NetworkObject(NetworkedPlayer relevantPlayer) => relevantPlayer.Object;
    public static explicit operator ClientNetworkTransform(NetworkedPlayer relevantPlayer) => relevantPlayer.Transform;
    public static explicit operator HandlePlayerNetworkBehaviour(NetworkedPlayer relevantPlayer) => relevantPlayer.BehaviourHandler;
    public static explicit operator WeaponHandler(NetworkedPlayer relevantPlayer) => relevantPlayer.WeaponHandler;
    public static explicit operator PlayerHealthNetworked(NetworkedPlayer relevantPlayer) => relevantPlayer.Health; 

    #endregion

    #endregion
}

[Serializable]
public struct NetworkedPlayerPrimitive : INetworkSerializable
{
    public string Name;
    public ushort TeamID;
    public ulong ObjectNetworkID;

    public NetworkedPlayerPrimitive(string name, ushort teamID, ulong objectNetworkID)
    {
        Name = name;
        TeamID = teamID;
        ObjectNetworkID = objectNetworkID;
    }

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref Name);
        serializer.SerializeValue(ref TeamID);
        serializer.SerializeValue(ref ObjectNetworkID);
    }

    public readonly NetworkedPlayer ToNetworkedPlayer()
    {
        NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(ObjectNetworkID, out var networkObject);
        return new NetworkedPlayer(
                Name,
                TeamID,
                networkObject,
                networkObject.GetComponent<ClientNetworkTransform>(),
                networkObject.GetComponent<HandlePlayerNetworkBehaviour>(),
                networkObject.GetComponent<WeaponHandler>(),
                networkObject.GetComponent<PlayerHealthNetworked>()
            );
    }

}

public enum NetworkedComponent : byte
{
    NetworkObject,
    ClientNetworkTransform,
    HandlePlayerNetworkBehaviour,
    WeaponHandler,
    PlayerHealthNetworked
}
