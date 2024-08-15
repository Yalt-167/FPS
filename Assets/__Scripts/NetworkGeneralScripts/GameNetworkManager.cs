//#define DEBUG_MULTIPLAYER
#define LOG_METHOD_CALLS

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using UnityEngine;
using Unity.Multiplayer.Samples.Utilities.ClientAuthority;
using Unity.Netcode;

using UnityEditor;

using Random = UnityEngine.Random;
using static DebugUtility;

namespace GameManagement
{
    [DefaultExecutionOrder(-99)]
    public sealed class GameNetworkManager : NetworkManager
    {
        public static GameNetworkManager Manager;
        [SerializeField] private GameObject gameManagerPrefab;
        private GameObject gameManagerInstance;

        #region Unity Handled

        private void Awake()
        {
            Manager = this;
            OnServerStarted += CreateManagerInstance;
            OnServerStopped += KillManagerInstance;
        }

        #endregion

        #region Initialization & Cleanup

        private void CreateManagerInstance()
        {
            gameManagerInstance = Instantiate(gameManagerPrefab);
            gameManagerInstance.GetComponent<NetworkObject>().Spawn();
        }

        private void KillManagerInstance(bool param)
        {
            Destroy(gameManagerInstance);
        }

        #endregion

        #region Player List

        public NetworkedPlayer[] Players => players;
        private NetworkedPlayer[] players;

        public void DisconnectPlayer(ushort playerID)
        {
            players[playerID].Online = false;
        }

        public void ReconnectPlayer(ushort playerID)
        {
            players[playerID].Online = true;
        }

        /// <summary>
        /// <paramref name="whichComponentID"/> basically refers to which component ID was passed in the method.<br/>
        /// For instance if we have is the weaponHandlerand passed its ID we should also pass the relevant enum member 
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

        public NetworkedPlayer RetrievePlayerFromIndex(int index)
        {
            return players[index];
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
            foreach (var player in players)
            {
                if (player.Name == name)
                {
                    return player;
                }
            }

            return null;
        }

        [MenuItem("Developer/DebugPlayerList")]
        public static void StaticDebugPlayerList()
        {
            Manager.DebugPlayerList();
        }

        private void DebugPlayerList()
        {
            if (players == null) { Debug.Log("Players is null");  return; }

            var stringBuilder = new StringBuilder();

            stringBuilder.Append("[ ");
            var isFirst = true;
            foreach (var player in players)
            {
                stringBuilder.Append(isFirst ? $"{player.GetInfos()}" : $", {player.GetInfos()}");
                isFirst = false;
            }
            stringBuilder.Append(" ]");

            Debug.Log(stringBuilder.ToString());
        }

        public void RetrievePlayerList()
        {
            players = GetNetworkedPlayers();
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
                Debug.Log($"NetworkObjectId: {element.Key} | Obj: {element.Value.name}");
            }
        }

        [MenuItem("Developer/DebugNetworkBehaviours")]
        public static void DebugNetworkBehaviours()
        {
            foreach (KeyValuePair<ulong, NetworkObject> element in NetworkManager.Singleton.SpawnManager.SpawnedObjects)
            {
                Debug.Log($"NetworkObjectId: {element.Key} | Obj: {element.Value.name}");
                foreach (NetworkBehaviour networkBehaviour in element.Value.GetComponents<NetworkBehaviour>())
                {
                    Debug.Log($"Behaviour: {networkBehaviour.GetType()} | NetworkBehaviourId: {networkBehaviour.NetworkBehaviourId}");
                }
            }
        }

        #endregion

        private NetworkedPlayer[] GetNetworkedPlayers()
        {
            //NetworkedPlayer[] players_ = new NetworkedPlayer[NetworkManager.Singleton.SpawnManager.SpawnedObjects.Count - 1]; // - 1 for the manager
            NetworkedPlayer[] players_ = new NetworkedPlayer[NetworkManager.Singleton.ConnectedClientsList.Count];

#if DEBUG_MULTIPLAYER
        var stringBuilder = new StringBuilder();
        stringBuilder.Append("[ ");
#endif
            ushort idx = 0;
            foreach (KeyValuePair<ulong, NetworkObject> element in NetworkManager.Singleton.SpawnManager.SpawnedObjects)
            {
#if DEBUG_MULTIPLAYER
            stringBuilder.Append($"{{{element.Key}, {element.Value}}} ");
#endif
                if (element.Value.TryGetComponent<PlayerFrame>(out var playerFrameComponent))
                {
                    players_[idx] = playerFrameComponent.AsNetworkedPlayer(idx++);
                }
            }
#if DEBUG_MULTIPLAYER
        stringBuilder.Append("]");
        print(stringBuilder.ToString());
#endif
            return players_;
        }


        [MenuItem("Developer/TestNames")]
        public static void TestNames()
        {
            foreach (KeyValuePair<ulong, NetworkObject> element in NetworkManager.Singleton.SpawnManager.SpawnedObjects)
            {
                if (element.Value.gameObject.TryGetComponent<PlayerFrame>(out var playerFrameComponent))
                {
                    Debug.Log(playerFrameComponent.Name);
                }
            }
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

        public override readonly string ToString()
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

        public readonly NetworkedPlayer AsNetworkedPlayer()
        {
#if DEBUG_MULTIPLAYER
        Debug.Log($"Name: {Name} | objectNetworkID: {ObjectNetworkID}");
#endif
            if (!NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(ObjectNetworkID, out var networkObject))
            {
                throw new Exception($"This player (ObjectNetworkID: {ObjectNetworkID}) was not properly spawned");
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
}