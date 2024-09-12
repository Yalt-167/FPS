//#define DEBUG_MULTIPLAYER
#define LOG_METHOD_CALLS

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using UnityEngine;
using Unity.Netcode;

using UnityEditor;

using Random = UnityEngine.Random;

namespace GameManagement
{
    [DefaultExecutionOrder(-99)]
    public sealed class GameNetworkManager : NetworkManager
    {
        public static GameNetworkManager Manager;
        [SerializeField] private GameObject gameManagerPrefab;
        private GameObject gameManagerInstance;

        [SerializeField] private GameObject teamSelectorPrefab;
        private GameObject teamSelectorInstance;

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

        public PlayerFrame[] Players => players;
        private PlayerFrame[] players;
        public int PlayerCount => Players.Length;

        public void DisconnectPlayer(ushort playerID)
        {
            players[playerID].IsOnline = false;
        }

        public void ReconnectPlayer(ushort playerID)
        {
            players[playerID].IsOnline = true;
        }

        /// <summary>
        /// <paramref name="whichComponentID"/> basically refers to which component ID was passed in the method.<br/>
        /// For instance if we have is the weaponHandlerand passed its ID we should also pass the relevant enum member 
        /// </summary>
        /// <param name="componentID"></param>
        /// <param name="whichComponentID"></param>
        public PlayerFrame RetrievePlayerFromComponentID(ulong componentID, NetworkedComponent whichComponentID)
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

        public PlayerFrame RetrievePlayerFromIndex(int index)
        {
            return players[index];
        }

        public IEnumerable<PlayerFrame> GetPlayers()
        {
            foreach (var player in players)
            {
                yield return player;
            }
        }

        public IEnumerable<PlayerFrame> GetPlayersOfTeam(ushort teamID)
        {
            foreach (var player in players)
            {
                if (player.TeamNumber == teamID)
                {
                    yield return player;
                }
            }
        }

        public bool PlayerWithNameExist(string name)
        {
            return GetPlayerFromName(name) != null;
        }

#nullable enable
        public PlayerFrame? GetPlayerFromName(string name)
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
#nullable disable

        [MenuItem("Developer/DebugPlayerList")]
        public static void StaticDebugPlayerList()
        {
            Manager.DebugPlayerList();
        }

        private void DebugPlayerList()
        {
            if (players == null) { Debug.Log("No player list RN");  return; }

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

        public void InitPlayerList()
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


        private PlayerFrame[] GetNetworkedPlayers()
        {

            PlayerFrame[] players_ = new PlayerFrame[NetworkManager.Singleton.SpawnManager.SpawnedObjects.Count - 1]; // - 1 for the manager
            //NetworkedPlayer[] players_ = new NetworkedPlayer[NetworkManager.Singleton.ConnectedClientsList.Count]; // not allowed to call on clients

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
                    players_[idx++] = playerFrameComponent;
                }
            }
#if DEBUG_MULTIPLAYER
        stringBuilder.Append("]");
        print(stringBuilder.ToString());
#endif
            return players_;
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

        #region Team Selector Thingy

        //[MenuItem("Developer/SpawnTeamSelectionMenu")]
        //public static void StaticSpawnTeamSelectionMenu()
        //{
        //    Manager.SpawnTeamSelectionMenu();
        //}

        public void SpawnTeamSelectionMenu(ushort teamsCount, ushort teamsSize)
        {
            if (teamSelectorInstance == null)
            {
                teamSelectorInstance = Instantiate(teamSelectorPrefab);
                teamSelectorInstance.GetComponent<NetworkObject>().Spawn();
            }

            var teamSelector = teamSelectorInstance.GetComponent<TeamSelector>();
            teamSelector.SetDataServerRpc(teamsCount, teamsSize);
            teamSelector.ToggleTeamSelectionScreenServerRpc(towardOn__: true);
        }

        #endregion
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


public struct NetworkSerializableString : INetworkSerializable
{
    public string Value;
    public NetworkSerializableString(string value) { Value = value; }
    public static implicit operator string(NetworkSerializableString value) { return value.Value; }
    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref Value);
    }
    public readonly override string ToString()
    {
        return Value;
    }
}

//public struct NetworkSerializableData<T> : INetworkSerializable where T : struct
//{
//    public T Data;
//    public NetworkSerializableData(T value) { Data = value; }
//    public static implicit operator T(NetworkSerializableData<T> value) { return value.Data; }
//    public void NetworkSerialize<Type>(BufferSerializer<Type> serializer) where Type : IReaderWriter
//    {
//        serializer.SerializeValue(ref Data);
//    }
//    public override string ToString()
//    {
//        return Data.ToString();
//    }
//}