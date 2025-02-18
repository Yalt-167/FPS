//#define DEBUG_MULTIPLAYER
#define LOG_METHOD_CALLS


using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Unity.Netcode;

using UnityEditor;
using UnityEngine;

using LobbyHandling;
using SceneHandling;

namespace GameManagement
{
    public sealed class Game : NetworkBehaviour
    {
        public static Game Manager;
        private static IGameRule currentGameRule;
        private readonly NetworkVariable<WinInfos> winInfos = new NetworkVariable<WinInfos>(readPerm: NetworkVariableReadPermission.Everyone, writePerm: NetworkVariableWritePermission.Server);

        #region Public References

        public static PlayerFrame[] Players => Manager.players;
        public static int PlayerCount { get; private set; }

        #endregion

        #region Unity Handled

        private void Awake()
        {
            Manager = this;

            GameNetworkManager.AssignGameManagerInstance(gameObject);
        }

        private void Update()
        {
            if (!IsServer) { return; }

            if (!Started) { return; }

            //Debug.Log("Game was Started");

            IClientSideGameRuleUpdateParam clientSideGameRuleUpdateParam = currentGameRule.UpdateGameServerSide();
            UpdateGameServerRpc(clientSideGameRuleUpdateParam);

            if (winInfos.Value.HasWinner())
            {
                currentGameRule.EndGameServerSide(winInfos.Value);
                EndGameServerRpc(winInfos.Value);
            }
        }

        #endregion


        #region GameRule Handling 

        [Rpc(SendTo.Server)]
        private void UpdateGameServerRpc(IClientSideGameRuleUpdateParam param)
        {
            UpdateGameClientRpc(param);
        }

        [Rpc(SendTo.ClientsAndHost)]
        private void UpdateGameClientRpc(IClientSideGameRuleUpdateParam param)
        {
            winInfos.Value = currentGameRule.UpdateGameClientSide(param);
        }

        [Rpc(SendTo.Server)]
        private void EndGameServerRpc(WinInfos param)
        {
            EndGameClientRpc(param);
        }

        [Rpc(SendTo.ClientsAndHost)]
        private void EndGameClientRpc(WinInfos param)
        {
            currentGameRule.EndGameClientSide(param);

            OnGeneralGameEndedClientSide?.Invoke();

            Started = false;
        }

        #endregion

        #region Respawn Logic

        private readonly List<List<SpawnPoint>> spawnPoints = new();

        public void InitiateSpawnPoints()
        {
            var teamsCount = GameNetworkManager.Manager.TeamSelectionScreen.TeamsCount;
            for (int i = 0; i < teamsCount; i++)
            {
                spawnPoints[i] = new List<SpawnPoint>();
            }
        }

        public void AddRespawnPoint(SpawnPoint spawnPoint)
        {
            if (spawnPoints.Count == 0) { InitiateSpawnPoints(); }

            spawnPoints[spawnPoint.TeamNumber - 1].Add(spawnPoint);
        }

        public void DiscardRespawnPoint(SpawnPoint spawnPoint)
        {
            spawnPoints[spawnPoint.TeamNumber - 1].Remove(spawnPoint);
        }

        public Vector3 GetSpawnPosition(ushort teamNumber)
        {
            var teamID = teamNumber - 1;
            var relevantSpawnPoints = spawnPoints[teamID];

            if (relevantSpawnPoints.Count == 0) { return NoSpawnpointAvailableForThisTeam(teamID); }

            var activeRelevantSpawnPoints = (List<SpawnPoint>)relevantSpawnPoints.Where(predicate: (spawnPoint) => spawnPoint.Active);

            if (activeRelevantSpawnPoints.Count == 0) { return NoSpawnpointAvailableForThisTeam(teamID); }

            return activeRelevantSpawnPoints[UnityEngine.Random.Range(0, activeRelevantSpawnPoints.Count)].SpawnPosition;
        }

        private Vector3 NoSpawnpointAvailableForThisTeam(int teamID)
        {
            print($"There s no checkpoint available for this player with team ID: {teamID}");
            return Vector3.zero;
        }

        #endregion

        #region Game Start

        public static bool Started { get; private set; }
        public static event Action OnGeneralGameStartedClientSide;
        public static event Action OnGeneralGameEndedClientSide;

        public static void SetupOnAllClients()
        {
            Manager.SetupServerRpc();
        }

        [Rpc(SendTo.Server)]
        private void SetupServerRpc()
        {
            SetupClientRpc(NetworkManager.Singleton.ConnectedClientsIds.Count);
        }

        [Rpc(SendTo.ClientsAndHost)]
        private void SetupClientRpc(int connectedClientCount)
        {
            SceneLoader.LoadScene(Scenes.HUD.MainScoreboardHUD, SceneType.HUD);
            PlayerCount = connectedClientCount;
        }



        #region Start/End Game

        [MenuItem("Developer/StartGame")]
        public static void StartGame()
        {
            if (!Manager.IsServer) { return; }

            Manager.StartGameServerRpc();
        }

        [Rpc(SendTo.Server)]
        private void StartGameServerRpc()
        {
            currentGameRule.StartGameServerSide();
            StartGameClientRpc();
        }

        [Rpc(SendTo.ClientsAndHost)]
        private void StartGameClientRpc()
        {
            OnGeneralGameStartedClientSide?.Invoke();

            currentGameRule.StartGameClientSide();

            LobbyHandler.Instance.ToggleMenuCamera(false);

            PlayerFrame.LocalPlayer.SetGameplayInputMode();
            PlayerFrame.LocalPlayer.ToggleHUD(true);

            Started = true;
        }

        #endregion

        #region Create Player List

        [MenuItem("Developer/CreatePlayerList")]
        public static void CreatePlayerListOnAllClients()
        {
            Manager.CreatePlayerListServerRpc();
        }

        [Rpc(SendTo.Server)]
        private void CreatePlayerListServerRpc()
        {
            CreatePlayerListClientRpc();
        }

        [Rpc(SendTo.ClientsAndHost)]
        private void CreatePlayerListClientRpc()
        {
            InitPlayerList();
        }

        #endregion

        #region Lobby Menu

        public static void ToggleLobbyMenuOnAllClients(bool towardOn)
        {
            Manager.ToggleLobbyMenuServerRpc(towardOn);
        }

        [Rpc(SendTo.Server)]
        private void ToggleLobbyMenuServerRpc(bool towardOn)
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
            
        }

        public static void OnClientDisconnected(ulong clientID)
        {

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

        [MenuItem("Developer/Debug/DebugPlayerList")]
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

        #region Level Loading

        public static void LoadLevelOnAllClients(string gamemode, string map)
        {
            Manager.LoadGameRuleServerRpc(gamemode);

            Manager.LoadMapServerRpc(Scenes.GetSceneFromGamemodeAndMap(gamemode, map));
        }

        [Rpc(SendTo.Server)]
        private void LoadGameRuleServerRpc(string gameMode)
        {
            LoadGameRuleClientRpc(gameMode);
        }

        [Rpc(SendTo.ClientsAndHost)]
        private void LoadGameRuleClientRpc(string gameMode)
        {
            var relevantGameRuleType = gameMode switch
            {
                nameof(GameModes.TeamFight) => typeof(TeamFightGameRule),
                nameof(GameModes.DeathMatch) => null,
                nameof(GameModes.CaptureTheFlag) => null,
                nameof(GameModes.HardPoint) => null,
                nameof(GameModes.Escort) => null,
                nameof(GameModes.Arena) => null,
                nameof(GameModes.Breakthrough) => null,
                _ => throw new Exception($"This gamemode ({gameMode}) does not exist")
            };

            currentGameRule = (IGameRule)GameNetworkManager.Manager.GameManagerInstance.GetComponent(relevantGameRuleType) ?? throw new Exception("[LoadGamerule] GetComponent() call failed");
        }


        [Rpc(SendTo.Server)]
        private void LoadMapServerRpc(string map) // handle the map meta data with an SO
        {
            LoadMapClientRpc(map);
        } 

        [Rpc(SendTo.ClientsAndHost)]
        private void LoadMapClientRpc(string map)
        {
            SceneLoader.UnloadScene(Scenes.LoginScene, SceneType.Map);
            SceneLoader.LoadScene(map, SceneType.Map);
        }

        #endregion
    }
}

// rework camera
// {
//      - get rid of that camera roll
//      - fix the slide camera bc it s goofy so far
// }

// rework guns
// {
//      add a level of inheritance so PlayerCombat only calls HeldItem.OnHold|OnOnRelease|etc so the HeldItem can be switched independantly
//      separate file for each wap?(would allow to handle pull out time and each HeldItem cooldown can be internal) -> link to the relevant shooting style/ryhtm using using strategy pattern)

// pull out ADS logic
// add tw anchors (no ADS / ADS) and lerp from one to another with the guns transforms

// pull out Kickback and recoil logic in separate scripts too

// redo the hierarchy for gun as such
// hand socket
// {

// - barrel ends holder
//      {
//          - as many barrel ends as I want so they can be picked up dynamically using transform.GetChild() calls
//      }
// - weapon
//      {
//          - actual weapon model hierarchy
//      }
// }


// + dual wielding
// each shot cycle the transform from which the shot is shot (separate class for barrel end -> call Wap.GetBarrelEnd() and do that
// for ADS double the ADS anchors (separate class ADS anchor that could handle that internally)
