//#define DEBUG_MULTIPLAYER
#define LOG_METHOD_CALLS

using LobbyHandling;
using SceneHandling;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Unity.Netcode;
using UnityEditor;
using UnityEngine;

using Random = UnityEngine.Random;

namespace GameManagement
{
    public sealed class Game : NetworkBehaviour
    {
        public static Game Manager;

        #region References

        public static PlayerFrame[] Players => Manager.players;
        public static int PlayerCount { get; private set; }

        #endregion



        #region Unity Handled

        private void Awake()
        {
            Manager = this;
        }

        #endregion

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
            Debug.Log($"There s no checkpoint available for this player with team ID: {teamID}");
            return Vector3.zero;
        }

        #endregion

        #region Game Start

        public static bool Started { get; private set; }
        public static event Action OnGameStarted;

        [MenuItem("Developer/StartGame")]
        public static void StartGame()
        {
            Manager.StartGameServerRpc();
        }

        [Rpc(SendTo.Server)]
        private void StartGameServerRpc()
        {
            if (!Started)
            {
                StartGameClientRpc();
            }
        }

        [Rpc(SendTo.ClientsAndHost)]
        private void StartGameClientRpc()
        {
            Started = true;
            OnGameStarted?.Invoke();

            Debug.Log("Game was started");

            LobbyHandler.Instance.ToggleMenuCamera(false);

            PlayerFrame.LocalPlayer.SetGameplayInputMode();
            PlayerFrame.LocalPlayer.ToggleHUD(true);
        }


        [MenuItem("Developer/CreatePlayerList")]
        public static void StaticCreatePlayerList()
        {
            Manager.CreatePlayerListServerRpc();
        }

        [Rpc(SendTo.Server)]
        public void CreatePlayerListServerRpc()
        {
           CreatePlayerListClientRpc();
        }

        [Rpc(SendTo.ClientsAndHost)]
        private void CreatePlayerListClientRpc()
        {
            InitPlayerList();
        }


        #region Lobby Menu

        [Rpc(SendTo.Server)]
        public void ToggleLobbyMenuServerRpc(bool towardOn)
        {
            ToggleLobbyMenuClientRpc(towardOn);
        }

        [Rpc(SendTo.ClientsAndHost)]
        private void ToggleLobbyMenuClientRpc(bool towardOn)
        {
            LobbyHandler.Instance.ToggleLobbyMenu(towardOn);
        }

        #endregion

        #endregion

        #region Player List

        private PlayerFrame[] players;
        public static void OnClientConnected(ulong clientID)
        {
            PlayerCount++;
        }

        public static void OnClientDisconnected(ulong clientID)
        {
            PlayerCount--;
        }


        /// <summary>
        /// <paramref name="whichComponentID"/> basically refers to which component ID was passed in the method.<br/>
        /// For instance if we have is the weaponHandlerand passed its ID we should also pass the relevant enum member 
        /// </summary>
        /// <param name="objectNetworkID"></param>
        /// <param name="whichComponentID"></param>
        public static PlayerFrame RetrievePlayerFromNetworkObjectID(ulong objectNetworkID)
        {
            return Manager.players.First(predicate: (each) => each.NetworkObjectId == objectNetworkID);
        }

        public static IEnumerable<PlayerFrame> GetPlayersOfTeam(ushort teamID)
        {
            foreach (var player in Manager.players)
            {
                if (player.TeamNumber == teamID)
                {
                    yield return player;
                }
            }
        }

#nullable enable
        public static PlayerFrame? GetPlayerFromName(string name)
        {
            foreach (var player in Manager.players)
            {
                if (player.Name == name)
                {
                    return player;
                }
            }

            return null;
        }
#nullable disable

        #region Debug Player List

        [MenuItem("Developer/DebugPlayerList")]
        public static void StaticDebugPlayerList()
        {
            Manager.DebugPlayerList();
        }

        private void DebugPlayerList()
        {
            if (players == null) { Debug.Log("No player list RN"); return; }

            var stringBuilder = new StringBuilder();

            _ = stringBuilder.Append("[ ");
            var isFirst = true;
            foreach (var player in players)
            {
                _ = stringBuilder.Append(isFirst ? $"{player.GetInfos()}" : $", {player.GetInfos()}");
                isFirst = false;
            }
            _ = stringBuilder.Append(" ]");

            Debug.Log(stringBuilder.ToString());
        }

        #endregion

        public static void InitPlayerList()
        {
            Manager.players = new PlayerFrame[PlayerCount];
            //Manager.players = new PlayerFrame[NetworkManager.Singleton.SpawnManager.SpawnedObjects.Count - 1]; // - 1 for the manager

#if DEBUG_MULTIPLAYER
        var stringBuilder = new StringBuilder();
        _ = stringBuilder.Append("[ ");
#endif
            var idx = 0;
            foreach (KeyValuePair<ulong, NetworkObject> element in NetworkManager.Singleton.SpawnManager.SpawnedObjects)
            {
#if DEBUG_MULTIPLAYER
            _ = stringBuilder.Append($"{{{element.Key}, {element.Value}}} ");

#endif
                if (element.Value.TryGetComponent<PlayerFrame>(out var playerFrameComponent))
                {
                    Manager.players[idx++] = playerFrameComponent;
                }
            }
#if DEBUG_MULTIPLAYER
        _ = stringBuilder.Append("]");
        print(stringBuilder.ToString());
#endif
        }

        #endregion

        #region Map Loading

        [Rpc(SendTo.Server)]
        public void LoadMapServerRpc(string map)
        {
            LoadMapClientRpc(map);
        }

        [Rpc(SendTo.ClientsAndHost)]
        private void LoadMapClientRpc(string map)
        {
            SceneLoader.Instance.LoadScene(map, additive: false);
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
}

// abstract class GameRule that handles the game flow

// every gamemode has it s own derived class that handles the flow of the game

// necessarily has
// EntryPoint => OnStart()
// fetch the spawnpoints and important things to the map
// -> DataStructure for each map then prolly a SO


// EndPoint => OnEnd()

// loadMap takes in a SO representing the map
// the SO has the scene in its member along with useful data about the map