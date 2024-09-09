//#define LOG_LOBBY_EVENTS
#define DEV_BUILD

using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Reflection;

using UnityEditor;
using UnityEngine;

using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using Unity.Services.Relay.Models;
using Unity.Services.Relay;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;

using GameManagement;


namespace LobbyHandling
{
    public sealed class LobbyHandler : MonoBehaviour
    {
        public static LobbyHandler Instance { get; private set; }
        public int SpaceBetweenButtons = 12;

        private Lobby hostLobby;
        private bool isCreatingLobby;
#if DEV_BUILD
        private bool startedALocalTestingSession;
        private bool inALocalTestingSession;
#endif
        private static readonly float heartbeat = 15f; // what pings the lobby for it to stay active when not interacted with (in seconds)
        private float heartbeatTimer;

        private static readonly float lobbyUpdateRate = 2.5f; // how often the lobby updates (in seconds)
        private float lobbyUpdateTimer;

        private static readonly string noPassword = "        ";
        //private static readonly string initializeToZero = "0";

        public string ProfileName;
        private bool isSignedIn;
        private bool isSigningIn;
        private Player localPlayer;
        private Camera menuCamera;

        #region Lobby list

#nullable enable
        private QueryResponse? availableLobbies;
#nullable disable
        private bool isSearchingForLobbies;
        private bool canRefreshLobbyList = true;
        [SerializeField] private float cooldownBeforeCanRefreshLobbyList;
        private Vector2 scrollPosition = Vector2.zero;
        private string passwordToJoinListLobby;

        #endregion

        #region Filters Handling

        public FiltersValuesStruct FiltersValues;
        public FiltersStruct Filters;

        private void InitializeFilters()
        {
            FiltersValues = new()
            {
                GameMode = DataObject.IndexOptions.S1,
                IsRanked = DataObject.IndexOptions.S2,
                Map = DataObject.IndexOptions.S3,
                Region = DataObject.IndexOptions.S4,
            };

            Filters = new()
            {
                GameMode = QueryFilter.FieldOptions.S1,
                IsRanked = QueryFilter.FieldOptions.S2,
                Map = QueryFilter.FieldOptions.S3,
                Region= QueryFilter.FieldOptions.S4,
            };

        }

        public struct FiltersValuesStruct
        {
            public DataObject.IndexOptions GameMode;
            public DataObject.IndexOptions HasPassword;
            public DataObject.IndexOptions IsRanked;
            public DataObject.IndexOptions Map;
            public DataObject.IndexOptions Region;
        }

        public struct FiltersStruct
        {
            public QueryFilter.FieldOptions GameMode;
            public QueryFilter.FieldOptions HasPassword;
            public QueryFilter.FieldOptions IsRanked;
            public QueryFilter.FieldOptions Map;
            public QueryFilter.FieldOptions Region;
        }

        #endregion

        #region Init

        private async void Start()
        {
           InitializeLobbyHandler();

            InitializeFilters();

            await UnityServices.InitializeAsync();

            AuthenticationService.Instance.SignedIn += SignInCallback;
            AuthenticationService.Instance.SignInFailed += SignInFailedCallback;

            AuthenticationService.Instance.SignedOut += SignOutCallback;
            AuthenticationService.Instance.Expired += SessionExpiredCallback;

            InitializeUnityTransports();

            LobbyMenuActive = true;
        }

        private void InitializeLobbyHandler()
        {
            Instance = this;

            menuCamera = transform.GetChild(0).GetComponent<Camera>();


            UpdateDropdownOptions(typeof(GameModes), ref gameModesDropDown);

            currentMapDropDownModelType = typeof(Maps);
            UpdateDropdownOptions(typeof(Maps), ref mapsDropDown);
        }

        public async void SignIn()
        {
            isSigningIn = true;

            AuthenticationService.Instance.SwitchProfile(ProfileName);

            await AuthenticationService.Instance.SignInAnonymouslyAsync();

            localPlayer = GetPlayer();
        }

        #region Sign in/Out Callbacks

        private void SignInCallback()
        {
            Debug.Log($"Signed in as {AuthenticationService.Instance.PlayerId}");
            isSignedIn = true;
        }

        private void SignInFailedCallback(RequestFailedException exception)
        {
            Debug.Log(exception.Message);
        }

        private void SignOutCallback()
        {
            Debug.Log("Signed out");
            isSignedIn = false;
        }

        private void SessionExpiredCallback()
        {
            Debug.Log("Session Expired");
            isSignedIn = false;
        }

        #endregion

        public async void SignIn(SignInMethod signInMethod)
        {
            switch (signInMethod)
            {
                case SignInMethod.Anonymously:
                    await AuthenticationService.Instance.SignInAnonymouslyAsync();
                    break;

                case SignInMethod.Google:
                    //await AuthenticationService.Instance.SignInWithGoogleAsync(AuthenticationService.Instance.AccessToken);
                    break;
            }
        }

        public enum SignInMethod
        {
            Anonymously,
            Google
        }

        #endregion

        #region Update

        private void Update()
        {
            HandleHeartbeat();
            HandleLobbyUpdate();
        }

        private async void HandleHeartbeat()
        {
            if (!IsLobbyHost()) { return; }

            heartbeatTimer += Time.deltaTime;
            if (heartbeatTimer >= heartbeat)
            {
                heartbeatTimer = 0f;

                if (hostLobby == null) { return; }

                await LobbyService.Instance.SendHeartbeatPingAsync(hostLobby.Id);
            }
        }

        public async void HandleLobbyUpdate() // polling rate not linear ? when some guy presses tab -> query else no need?
        {
            if (hostLobby == null) { return; }

            if (Game.Manager.GameStarted) { return; }

            lobbyUpdateTimer += Time.deltaTime;
            if (lobbyUpdateTimer >= lobbyUpdateRate)
            {
                await UpdateLobbyAsync();
            }
        }

        public async void ForceLobbyUpdate()
        {
            if (hostLobby == null) { return; }

            await UpdateLobbyAsync();
        }

        public async Task UpdateLobbyAsync()
        {
            lobbyUpdateTimer = 0f;

            hostLobby = await LobbyService.Instance.GetLobbyAsync(hostLobby.Id);

#if LOG_LOBBY_EVENTS
            Debug.Log("Lobby was updated");
#endif
        }

        #endregion

        #region Lobby Actions

        [Header("Lobby settings")]
        public string LobbyName;
        public int LobbyCapacity;

        public bool PrivateLobby; // Private lobbies are NEVER visible in query results and require the lobby CODE or ID to be manually provided to new players.
        public string Password;

        [Header("Target lobby")]
        public string TargetLobbyCode;
        public string TargetLobbyID;

        [Header("Relay")]
        public string RelayJoinCode;

        public async void CreateLobby(string lobbyName, int lobbyCapacity, bool privateLobby, string password, string map, string gamemode)
        {
            var emptyPassword = string.IsNullOrEmpty(password);
            if (!emptyPassword && password.Length < 8)
            {
                Debug.Log("Your lobby was not created as your password doesn t have enough characters (at least 8)");
                return;
            }

            isCreatingLobby = true;
            var relaySuccessfullyCreated = await CreateRelayAsync(
#if HEADLESS_ARCHITECTURE_SERVER
            lobbyCapacity // is server so don t count as a player
#else
            lobbyCapacity - 1 // is host so both a player and the server and therefore should be counted
#endif
            );

            if (!relaySuccessfullyCreated)
            {
                Debug.Log("Your lobby was not created because the server failed to launch");
                return;
            }

            var lobbyOptions = new CreateLobbyOptions()
            {
                IsPrivate = privateLobby,
                Player = localPlayer,
                Password = emptyPassword ? noPassword : password,
                Data = new Dictionary<string, DataObject>()
                {
                    {
                        LobbyData.GameMode,
                        new DataObject(
                            visibility: DataObject.VisibilityOptions.Public,
                            value: gamemode,
                            index: FiltersValues.GameMode
                        )
                    },

                    {
                        LobbyData.Map,
                        new DataObject(
                            visibility: DataObject.VisibilityOptions.Public,
                            value: map,
                            index: FiltersValues.Map
                        )
                    },

                    {
                        LobbyData.Region,
                        new DataObject(
                            visibility: DataObject.VisibilityOptions.Public,
                            value: relayAllocation.Region,
                            index: FiltersValues.Region
                        )
                    },

                    {
                        LobbyData.RelayJoinCode,
                        new DataObject(
                            visibility: DataObject.VisibilityOptions.Public,
                            value: RelayJoinCode
                        )
                    },
                }
            };

            try
            {
                hostLobby = await LobbyService.Instance.CreateLobbyAsync(lobbyName, lobbyCapacity, lobbyOptions);
            }
            catch (LobbyServiceException exception)
            {
                Debug.Log($"Couldn t create a lobby. Reason: {exception.Message}");
                isCreatingLobby = false;
                return;
            }

            isCreatingLobby = false;
#if LOG_LOBBY_EVENTS
            Debug.Log($"Successfully created a new lobby");
            DisplayHostLobbyData();
#endif
            FillInLobbyDataFields();
            CopyLobbyID();
        }

        public async void EditLobby(string lobbyID, string lobbyName, int lobbyCapacity, bool privateLobby, string password, string map, string gamemode)
        {
            if (string.IsNullOrEmpty(lobbyID))
            {
                Debug.Log("No lobby ID provided");
                return;
            }

            if (!IsLobbyHost())
            {
                Debug.Log("You don t have permission for this");
                return;
            }

            var emptyPassword = string.IsNullOrEmpty(password);
            if (!emptyPassword && password.Length < 8)
            {
                Debug.Log("Your lobby was not created as your password doesn t have enough characters (at least 8)");
                return;
            }

            var updateOptions = new UpdateLobbyOptions()
            {
                Name = lobbyName,
                MaxPlayers = lobbyCapacity,
                IsPrivate = privateLobby,
                Password = emptyPassword ? noPassword : password,

                Data = new Dictionary<string, DataObject>() // idk how to represent that as param so far
                {
                    {
                        LobbyData.GameMode,
                        new DataObject(
                            visibility: DataObject.VisibilityOptions.Public,
                            value: gamemode,
                            index: FiltersValues.GameMode
                        )
                    },

                    {
                        LobbyData.Map,
                        new DataObject(
                            visibility: DataObject.VisibilityOptions.Public,
                            value: map,
                            index: FiltersValues.Map
                        )
                    },
                }
            };
            try
            {
                hostLobby = await LobbyService.Instance.UpdateLobbyAsync(lobbyID, updateOptions);
            }
            catch (LobbyServiceException exception)
            {
                Debug.Log($"Couldn t edit lobby: Reason: {exception.Message}");
                return;
            }
#if LOG_LOBBY_EVENTS
            Debug.Log($"Successfully edited lobby data");
            DisplayHostLobbyData();
#endif
        }

        public async void DeleteLobby(string lobbyID)
        {
            if (string.IsNullOrEmpty(lobbyID))
            {
                Debug.Log("No lobby ID provided");
                return;
            }

            if (!IsLobbyHost())
            {
                Debug.Log("You don t have permission for this");
                return;
            }

            try
            {
                await LobbyService.Instance.DeleteLobbyAsync(lobbyID);
            }
            catch (LobbyServiceException exception)
            {
                Debug.Log(exception.Message);
                return;
            }

#if LOG_LOBBY_EVENTS
            Debug.Log("Lobby was successfully deleted");
#endif

            hostLobby = null;
        }

        public async void QuickJoinLobby()
        {
            try
            {
                hostLobby = await LobbyService.Instance.QuickJoinLobbyAsync();
            }
            catch (LobbyServiceException exception)
            {
                Debug.Log(exception.Message);
                return;
            }

#if LOG_LOBBY_EVENTS
            Debug.Log($"Succesfully joined lobby: {hostLobby.Name}");
#endif

            JoinRelay(hostLobby.Data[LobbyData.RelayJoinCode].Value);
        }

        public async void JoinLobbyByID(string lobbyID, string password)
        {
            if (string.IsNullOrEmpty(lobbyID))
            {
                Debug.Log("No lobby ID provided");
                return;
            }

            var joinOptions = new JoinLobbyByIdOptions()
            {
                Player = localPlayer,
                Password = string.IsNullOrEmpty(password) ? noPassword : password
            };

            try
            {
                hostLobby = await LobbyService.Instance.JoinLobbyByIdAsync(lobbyID, joinOptions);
            }
            catch (LobbyServiceException exception)
            {
                Debug.Log(exception.Message);
                return;
            }

#if LOG_LOBBY_EVENTS
            Debug.Log($"Succesfully joined lobby: {hostLobby.Name}");
#endif

            JoinRelay(hostLobby.Data[LobbyData.RelayJoinCode].Value);
        }

        public async void JoinLobbyByCode(string lobbyCode, string password)
        {
            if (string.IsNullOrEmpty(lobbyCode))
            {
                Debug.Log("No lobby code provided");
                return;
            }
            var joinOptions = new JoinLobbyByCodeOptions()
            {
                Player = localPlayer,
                Password = string.IsNullOrEmpty(password) ? noPassword : password
            };

            try
            {
                hostLobby = await LobbyService.Instance.JoinLobbyByCodeAsync(lobbyCode, joinOptions);
            }
            catch (LobbyServiceException exception)
            {
                Debug.Log(exception.Message);
                return;
            }

#if LOG_LOBBY_EVENTS
            Debug.Log($"Succesfully joined lobby: {hostLobby.Name}");
#endif

            JoinRelay(hostLobby.Data[LobbyData.RelayJoinCode].Value);
        }

        public async void QuitLobby()
        {
            await QuitLobbyAsync(AuthenticationService.Instance.PlayerId);

            hostLobby = null;

            NetworkManager.Singleton.Shutdown();
        }

        public async Task QuitLobbyAsync(string playerID)
        {
            if (hostLobby == null)
            {
                Debug.Log("No lobby to quit");
                return;
            }

            try
            {
                await LobbyService.Instance.RemovePlayerAsync(hostLobby.Id, playerID);
            }
            catch (LobbyServiceException exception)
            {
                Debug.Log(exception.Message);
                return;
            }

//#if LOG_LOBBY_EVENTS // only relevant if was self quitting
//            Debug.Log("Successfully quit lobby");
//#endif
        }

        public async void CloseLobbyAcess()
        {
            if (hostLobby == null)
            {
                Debug.Log("You are not in a lobby");
                return;
            }

            if (!IsLobbyHost())
            {
                Debug.Log("You do not have permission for this");
                return;
            }

            var updateLobbyOptions = new UpdateLobbyOptions()
            {
                IsLocked = true,
                IsPrivate = true,
            };

            try
            {
                hostLobby = await LobbyService.Instance.UpdateLobbyAsync(hostLobby.Id, updateLobbyOptions);
            }
            catch (LobbyServiceException exception)
            {
                Debug.Log(exception.Message);
                return;
            }

#if LOG_LOBBY_EVENTS
            Debug.Log("Successfully closed the lobby");
#endif
        }

        public async void KickPlayer(string playerID)
        {
            if (!IsLobbyHost())
            {
                Debug.Log("You don t have permission for this");
                return;
            }

            await QuitLobbyAsync(playerID); // still has hostLobby set but IDk how to address that as of RN
            // try accessing a member field -> if cannot -> not in lobby -> remove hostLobby
        }

        public async void SetHost(string newHostPlayerID)
        {
            if (!IsLobbyHost())
            {
                Debug.Log("You don t have permission for this");
                return;
            }

            var updateOptions = new UpdateLobbyOptions()
            {
                HostId = newHostPlayerID
            };

            try
            {
                hostLobby = await LobbyService.Instance.UpdateLobbyAsync(hostLobby.Id, updateOptions);
            }
            catch (LobbyServiceException exception)
            {
                Debug.Log(exception.Message);
                return;
            }

#if LOG_LOBBY_EVENTS
            Debug.Log($"Successfully set new host");
#endif
        }

#nullable enable
        public async Task<QueryResponse?> EnumerateLobbiesAsync()
        {
            var options = new QueryLobbiesOptions()
            {
                Count = 25,
                Filters = new List<QueryFilter>()
                {
                    //new QueryFilter(QueryFilter.FieldOptions.Name, "that ll do for now", QueryFilter.OpOptions.EQ),
                    //new QueryFilter(QueryFilter.FieldOptions.AvailableSlots, "5", QueryFilter.OpOptions.LT),
                },
                Order = new List<QueryOrder>()
                {
                    new QueryOrder(false, QueryOrder.FieldOptions.AvailableSlots),
                }
            };

            QueryResponse response;

            try
            {
                response = await LobbyService.Instance.QueryLobbiesAsync(options);
            }
            catch (LobbyServiceException exception)
            {
                Debug.Log(exception.Message);
                return null;
            }

            return response.Results.Count > 0 ? response : null;
        }
#nullable disable
        private async void UpdateLobbyList()
        {
            if (!canRefreshLobbyList) { return; }

            await UpdateLobbyListInternalAsync();
        }

        private async Task UpdateLobbyListInternalAsync()
        {
            isSearchingForLobbies = true;

            availableLobbies = await EnumerateLobbiesAsync();

            isSearchingForLobbies = false;

            StartCoroutine(CooldownBeforeCanRefreshLobbyListAgain());
        }

        private IEnumerator CooldownBeforeCanRefreshLobbyListAgain()
        {
            canRefreshLobbyList = false;

            yield return new WaitForSeconds(cooldownBeforeCanRefreshLobbyList);

            canRefreshLobbyList = true;
        }

        private void FillInLobbyDataFields()
        {
            if (hostLobby == null) { return; };

            TargetLobbyCode = hostLobby.LobbyCode;
            TargetLobbyID = hostLobby.Id;
        }

        private void LaunchTeamSelectionMenu()
        {
            Game.Manager.ToggleLobbyMenuServerRpc(towardOn: false);
            Game.Manager.CreatePlayerListServerRpc();
            GameNetworkManager.Manager.SpawnTeamSelectionMenu(2, 6); // as it s a network spawn its automatically propagated to all clients
            Game.Manager.LoadMapServerRpc(mapsDropDown.Current);
        }

        #endregion

        #region Relay Handling

        private UnityTransport relayUnityTransport;
        private readonly int relayUnityTransportIndex = 0;
        private UnityTransport localUnityTransport;
        private readonly int localUnityTransportIndex = 1;

        private Allocation relayAllocation;

        private void InitializeUnityTransports()
        {
            var unityTransports = NetworkManager.Singleton.GetComponents<UnityTransport>();

            relayUnityTransport = unityTransports[relayUnityTransportIndex];

            localUnityTransport = unityTransports[localUnityTransportIndex];
        }

        public void SelectRelayUnityTransport()
        {
            DisableLocalUnityTransport();

            NetworkManager.Singleton.NetworkConfig.NetworkTransport = relayUnityTransport;

            EnableRelayUnityTransport();
        }
        public void EnableRelayUnityTransport()
        {
            relayUnityTransport.enabled = true;
        }
        public void DisableRelayUnityTransprt()
        {
            relayUnityTransport.enabled = false;
        }

        public void SelectLocalUnityTransport()
        {
            DisableRelayUnityTransprt();

            NetworkManager.Singleton.NetworkConfig.NetworkTransport = localUnityTransport;

            EnableLocalUnityTransport();
        }
        public void EnableLocalUnityTransport()
        {
            localUnityTransport.enabled = true;
        }
        public void DisableLocalUnityTransport()
        {
            localUnityTransport.enabled = false;
        }

        public async Task<bool> CreateRelayAsync(int slots)
        {
            try
            {
                relayAllocation = await RelayService.Instance.CreateAllocationAsync(slots);
                RelayJoinCode = await RelayService.Instance.GetJoinCodeAsync(relayAllocation.AllocationId);
            }
            catch (RelayServiceException exception)
            {
                Debug.Log(exception.Message);
                NetworkManager.Singleton.LogLevel = LogLevel.Developer;
                return false;
            }

            SelectRelayUnityTransport();
            relayUnityTransport.SetHostRelayData(
                relayAllocation.RelayServer.IpV4,
                (ushort)relayAllocation.RelayServer.Port,
                relayAllocation.AllocationIdBytes,
                relayAllocation.Key,
                relayAllocation.ConnectionData
            );

#if LOG_LOBBY_EVENTS
            Debug.Log("Successfully created relay");
#endif

            bool success =
#if HEADLESS_ARCHITECTURE_SERVER
            NetworkManager.Singleton.StartServer()
#else
            NetworkManager.Singleton.StartHost()
#endif
                ;

#if LOG_LOBBY_EVENTS
            Debug.Log(success ? "Server was successfully started" : "Failed to start server");
#endif

            return success;
        }

        public async void JoinRelay(string joinCode)
        {
            JoinAllocation joinAllocation;

            try
            {
                joinAllocation = await RelayService.Instance.JoinAllocationAsync(joinCode);
            }
            catch (RelayServiceException exception)
            {
                Debug.Log(exception.Message);
                return;
            }

            SelectRelayUnityTransport();
            relayUnityTransport.SetClientRelayData(
                joinAllocation.RelayServer.IpV4,
                (ushort)joinAllocation.RelayServer.Port,
                joinAllocation.AllocationIdBytes,
                joinAllocation.Key,
                joinAllocation.ConnectionData,
                joinAllocation.HostConnectionData
            );

            NetworkManager.Singleton.StartClient();

#if LOG_LOBBY_EVENTS
            Debug.Log("Successfully joined relay");
#endif
        }

        private async void ListRegionsData()
        {
            List<Region> regions;
            try
            {
                regions = await RelayService.Instance.ListRegionsAsync();
            }
            catch (RelayServiceException exception)
            {
                Debug.Log(exception.Message);
                return;
            }

            foreach (var region in regions)
            {
                Debug.Log(region.Description);
            }
        }

        #endregion

        #region Debug

        public async void DisplayLobbies()
        {
            var lobbies = await EnumerateLobbiesAsync();

            if (lobbies == null) { return; }

            Debug.Log($"Found {lobbies.Results.Count} lobb{(lobbies.Results.Count == 1 ? "y" : "ies")} matching your criteria, namely:");

            foreach (var lobby in lobbies.Results)
            {
                DisplayLobbyData(lobby);
            }   
        }

        public void DisplayHostLobbyData()
        {
            if (hostLobby == null) { return; }

            DisplayLobbyData(hostLobby);
        }

        public void DisplayLobbyData(Lobby lobby)
        {
            Debug.Log($"Name: {lobby.Name} | Capacity: {lobby.MaxPlayers} | Private: {lobby.IsPrivate}");
            Debug.Log($"ID: {lobby.Id} | Code: {lobby.LobbyCode}");

            foreach (KeyValuePair<string, DataObject> kvp in lobby.Data)
            {
                Debug.Log($"{kvp.Key}: {kvp.Value.Value}");
            }
        }

        public async void DisplayLobbyData(string lobbyID)
        {
            Lobby lobby;

            try
            {
                lobby = await LobbyService.Instance.GetLobbyAsync(lobbyID);
            }
            catch (LobbyServiceException exception)
            {
                Debug.Log(exception.Message);
                return;
            }

            DisplayLobbyData(lobby);
        }

        public void DisplayHostLobbyPlayers()
        {
            if (hostLobby == null) { return; }

            DisplayPlayers(hostLobby);
        }

        public void DisplayPlayers(Lobby lobby)
        {
            foreach (var player in lobby.Players)
            {
                Debug.Log($"{player.Data[PlayerDataForLobby.Username].Value}");
            }
        }

        public async void DisplayPlayers(string lobbyID)
        {
            Lobby lobby;

            try
            {
                lobby = await LobbyService.Instance.GetLobbyAsync(lobbyID);
            }
            catch (LobbyServiceException exception)
            {
                Debug.Log(exception.Message);
                return;
            }

            DisplayPlayers(lobby);
        }

        #endregion

        #region Utility

        public void CopyLobbyID()
        {
            if (hostLobby == null) { return; }

            GUIUtility.systemCopyBuffer = hostLobby.Id;
#if LOG_LOBBY_EVENTS
            Debug.Log("Lobby ID was copied to your clipboard");
#endif
        }

        public void CopyLobbyCode()
        {
            if (hostLobby == null) { return; }

            GUIUtility.systemCopyBuffer = hostLobby.LobbyCode;
#if LOG_LOBBY_EVENTS
            Debug.Log("Lobby code was copied to your clipboard");
#endif
        }

        public bool IsInLobby()
        {
            return hostLobby != null;
        }

        public bool IsLobbyHost()
        {
            return IsLobbyHost(localPlayer?.Id);
        }

        public bool IsLobbyHost(string playerID)
        {
            if (string.IsNullOrEmpty(playerID)) { return false; }

            if (hostLobby == null) { return false; }

            return hostLobby.HostId == playerID;
        }

#nullable enable
        public Player? GetLobbyHost()
        {
            if (hostLobby == null) { return null; }

            foreach (var player in hostLobby.Players)
            {
                if (player.Id == hostLobby.HostId)
                {
                    return player;
                }
            }

            return null; // CANNOT happen so should be fine
        }

        public Player? GetLobbyHost(Lobby lobby)
        {
            foreach (var player in lobby.Players)
            {
                if (player.Id == lobby.HostId)
                {
                    return player;
                }
            }

            return null; // means that the host quit and the lobby wasn t destroyed yet
        }
#nullable disable

        #endregion

        private Player GetPlayer()
        {
            return new Player(id: AuthenticationService.Instance.PlayerId)
                {
                    Data = new Dictionary<string, PlayerDataObject>()
                    {
                        { PlayerDataForLobby.Username, new PlayerDataObject(PlayerDataObject.VisibilityOptions.Public, ProfileName) },
                    }
                };
        }

        public void ToggleMenuCamera(bool towardOn)
        {
            menuCamera.enabled = towardOn;
        }

        #region GUI

        [field: Header("Lobby GUI Settings")]
        public bool LobbyMenuActive { get; private set; }
        [SerializeField] private int labelWidth;
        [SerializeField] private int fieldWidth;
        private GUIStyle titleLabelStyle;
        private GUIStyle smallerTitleLabelStyle;
        private GUIStyle warningLabelStyle;

        private void UpdateLabelStyles()
        {
            titleLabelStyle = new GUIStyle(GUI.skin.label)
            {
                fontStyle = FontStyle.Bold,
                fontSize = 24,
            };

            smallerTitleLabelStyle = new GUIStyle(GUI.skin.label)
            {
                fontStyle = FontStyle.Bold,
                fontSize = 16,
            };

            warningLabelStyle = new GUIStyle(GUI.skin.label)
            {
                fontStyle = FontStyle.Bold,
                fontSize = 10,
            };
        }

        private void CreateLobbyMenu()
        {
            GUI.enabled = isSignedIn;
            GUILayout.BeginVertical(CachedGUIStylesNames.Box);
            
            GUILayout.BeginHorizontal();
            GUILayout.Label(LobbyGUILabels.CreateYourLobby, titleLabelStyle);
            if (!isSignedIn)
            {
                GUILayout.Label(LobbyGUILabels.RequiresSignIn, warningLabelStyle);
            }
            GUILayout.EndHorizontal();

            DrawLobbySettings();

            GUI.enabled = isSignedIn && !isCreatingLobby;
            if ((GUILayout.Button(isCreatingLobby ? "Creating your lobby" : LobbyGUILabels.CreateLobby) || Input.GetKeyDown(KeyCode.Return)) && isSignedIn)
            {
                CreateLobby(LobbyName, LobbyCapacity, PrivateLobby, Password, mapsDropDown.Current, gameModesDropDown.Current); 
            }

            GUILayout.EndVertical();
            GUI.enabled = true;
        }

        private void EditLobbyMenu()
        {
            GUILayout.BeginVertical(CachedGUIStylesNames.Box);

            GUILayout.Label(LobbyGUILabels.EditYourLobby, titleLabelStyle);

            DrawLobbySettings();

            if (GUILayout.Button(LobbyGUILabels.EditLobby))
            {
                EditLobby(hostLobby.Id, LobbyName, LobbyCapacity, PrivateLobby, Password, mapsDropDown.Current, gameModesDropDown.Current);
            }

            GUILayout.Space(SpaceBetweenButtons);

            if (GUILayout.Button(LobbyGUILabels.CloseLobbyAccess))
            {
                CloseLobbyAcess();
            }

            GUILayout.Space(SpaceBetweenButtons);

            if (GUILayout.Button(LobbyGUILabels.DeleteLobby))
            {
                DeleteLobby(hostLobby.Id);
            }

            GUILayout.EndVertical();

            GUILayout.Space(SpaceBetweenButtons * 2);

            GUILayout.BeginVertical(CachedGUIStylesNames.Box);

            GUILayout.Label(LobbyGUILabels.ShareYourLobbyToAFriend, titleLabelStyle);
            if (GUILayout.Button(LobbyGUILabels.CopyLobbyID))
            {
                CopyLobbyID();
            }

            if (GUILayout.Button(LobbyGUILabels.CopyLobbyCode))
            {
                CopyLobbyCode();
            }

            GUILayout.EndVertical();
        }

        private void DrawLobbySettings()
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(LobbyGUILabels.LobbyName, GUILayout.Width(labelWidth));
            LobbyName = GUILayout.TextField(LobbyName, GUILayout.Width(fieldWidth));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label(LobbyGUILabels.LobbyCapacity, GUILayout.Width(labelWidth));
            LobbyCapacity = int.Parse(GUILayout.TextField(LobbyCapacity.ToString(), GUILayout.Width(fieldWidth)));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            PrivateLobby = GUILayout.Toggle(PrivateLobby, LobbyGUILabels.MakeLobbyPrivate);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label(LobbyGUILabels.LobbyPassword, GUILayout.Width(labelWidth));
            Password = GUILayout.PasswordField(Password, LobbyGUILabels.CensoredChar, GUILayout.Width(fieldWidth));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label(LobbyGUILabels.SelectAGameMode, GUILayout.Width(labelWidth));
            DropdownMenu(ref gameModesDropDown);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label(LobbyGUILabels.SelectAMap, GUILayout.Width(labelWidth));
            var relevantTypeModelForMapDropDown = Maps.GetRelevantTypeForMapOfGamemode(gameModesDropDown.Current);
            if (currentMapDropDownModelType != relevantTypeModelForMapDropDown)
            {
                currentMapDropDownModelType = relevantTypeModelForMapDropDown;
                UpdateDropdownOptions(currentMapDropDownModelType, ref mapsDropDown);
            }
            DropdownMenu(ref mapsDropDown);
            GUILayout.EndHorizontal();
        }

#if DEV_BUILD
        private void LocalTestingMenu()
        {
            GUILayout.BeginVertical(CachedGUIStylesNames.Box);

            GUILayout.Label(LobbyGUILabels.LocalTesting, titleLabelStyle);

            if (startedALocalTestingSession)
            {
                if (GUILayout.Button("Launch team selection menu"))
                {
                    LaunchTeamSelectionMenu();
                }
            }
            else if (inALocalTestingSession)
            {
                GUILayout.Label("You are hopefully in :)");
            }
            else
            {
                if (GUILayout.Button(LobbyGUILabels.StartALocalSession))
                {
                    SelectLocalUnityTransport();
                    startedALocalTestingSession = GameNetworkManager.Singleton.StartHost();
                }

                if (GUILayout.Button(LobbyGUILabels.JoinALocalSession))
                {
                    SelectLocalUnityTransport();
                    inALocalTestingSession = GameNetworkManager.Singleton.StartClient();
                }
            }

            GUILayout.EndVertical();
        }
#endif
        private void JoinLobbyMenu()
        {
            GUI.enabled = isSignedIn;
            GUILayout.BeginVertical(CachedGUIStylesNames.Box);

            GUILayout.BeginHorizontal();
            GUILayout.Label(LobbyGUILabels.JoinAFriendLobby, titleLabelStyle);
            if (!isSignedIn)
            {
                GUILayout.Label(LobbyGUILabels.RequiresSignIn, warningLabelStyle);
            }
            GUILayout.EndHorizontal();
            

            GUILayout.BeginHorizontal();
            GUILayout.Label(LobbyGUILabels.TargetLobbyCode, GUILayout.Width(labelWidth));
            TargetLobbyCode = GUILayout.TextField(TargetLobbyCode, GUILayout.Width(fieldWidth));
            GUILayout.EndHorizontal();
            if (GUILayout.Button(LobbyGUILabels.JoinLobbyByCode))
            {
                if (string.IsNullOrEmpty(TargetLobbyCode))
                {
                    Debug.Log(LobbyGUILabels.YouMustProvideACode);
                }
                else
                {
                    JoinLobbyByCode(TargetLobbyCode, Password);
                }
            }

            GUILayout.Space(SpaceBetweenButtons);

            GUILayout.BeginHorizontal();
            GUILayout.Label(LobbyGUILabels.TargetLobbyID, GUILayout.Width(labelWidth));
            TargetLobbyID = GUILayout.TextField(TargetLobbyID, GUILayout.Width(fieldWidth));
            GUILayout.EndHorizontal();
            if (GUILayout.Button(LobbyGUILabels.JoinLobbyByID))
            {
                if (string.IsNullOrEmpty(TargetLobbyID))
                {
                    Debug.Log(LobbyGUILabels.YouMustProvideAnID);
                }
                else
                {
                    JoinLobbyByID(TargetLobbyID, Password);
                }
            }

            GUILayout.Space(SpaceBetweenButtons);

            if (GUILayout.Button(LobbyGUILabels.JoinLobbyWithClipboard))
            {
                if (string.IsNullOrEmpty(GUIUtility.systemCopyBuffer))
                {
                    Debug.Log(LobbyGUILabels.SeemsLikeYourClipboardIsEmpty);
                }
                else
                {
                    JoinLobbyByID(GUIUtility.systemCopyBuffer, Password);
                }
            }

            GUILayout.EndVertical();
            GUI.enabled = true;
        }

        private void DisplayCurrentLobbyMenu()
        {
            if (hostLobby == null) { return; }

            GUILayout.BeginVertical(CachedGUIStylesNames.Box);

                GUILayout.BeginHorizontal();
                GUILayout.Label(LobbyGUILabels.CurrentLobbyColon, titleLabelStyle);
                GUILayout.Label($"{hostLobby.Name}");
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                GUILayout.Label(LobbyGUILabels.HostColon, smallerTitleLabelStyle);
                GUILayout.Label($"{GetLobbyHost().Data[PlayerDataForLobby.Username].Value}");
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                GUILayout.Label(LobbyGUILabels.GameMode, smallerTitleLabelStyle);
                GUILayout.Label($"{hostLobby.Data[LobbyData.GameMode].Value}");
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                GUILayout.Label(LobbyGUILabels.Map, smallerTitleLabelStyle);
                GUILayout.Label($"{hostLobby.Data[LobbyData.Map].Value}");
                GUILayout.EndHorizontal();

            GUILayout.EndVertical();

            GUILayout.BeginVertical(CachedGUIStylesNames.Box);

                GUILayout.BeginHorizontal();
                GUILayout.Label(LobbyGUILabels.PlayersColon, smallerTitleLabelStyle);
                GUILayout.EndHorizontal();

                foreach (var player in hostLobby.Players)
                {
                    GUILayout.Label(player.Data[PlayerDataForLobby.Username].Value);
                }

            GUILayout.EndVertical();

            GUILayout.BeginVertical(CachedGUIStylesNames.Box);

                if (IsLobbyHost())
                {
                    if (GUILayout.Button(LobbyGUILabels.CloseLobbyAndStartTeamSelection) || Input.GetKeyDown(KeyCode.Return))
                    {
                        CloseLobbyAcess();
                        LaunchTeamSelectionMenu();
                    }
                }
                else
                {
                    if (GUILayout.Button(LobbyGUILabels.QuitLobby))
                    {
                        QuitLobby();
                    }
                }

            GUILayout.EndVertical();
        }

#nullable enable
        private void DisplayAvailableLobbiesMenu()
        {     
            GUI.enabled = isSignedIn;
            GUILayout.BeginVertical(CachedGUIStylesNames.Box);

            GUILayout.BeginHorizontal();
            GUILayout.Label(LobbyGUILabels.Lobbies, titleLabelStyle);
            if (!isSignedIn)
            {
                GUILayout.Label(LobbyGUILabels.RequiresSignIn, warningLabelStyle);
            }
            GUILayout.EndHorizontal();

            GUILayout.BeginVertical();

            if (isSignedIn)
            {
                if (availableLobbies == null)
                {
                    GUILayout.BeginHorizontal(CachedGUIStylesNames.Box);
                    GUILayout.Label(LobbyGUILabels.NoLobbyFound);
                    if (!isSearchingForLobbies && canRefreshLobbyList && GUILayout.Button(LobbyGUILabels.SearchForLobbies))
                    {
                        UpdateLobbyList();
                    }
                    GUILayout.EndHorizontal();
                }
                else
                {
                    scrollPosition = GUILayout.BeginScrollView(scrollPosition);

                    foreach (var lobby in availableLobbies.Results)
                    {
                        Rect rect = EditorGUILayout.BeginVertical(CachedGUIStylesNames.Box);

                        if (GUI.Button(rect, GUIContent.none))
                        {
                            JoinLobbyByID(lobby.Id, string.IsNullOrEmpty(passwordToJoinListLobby) ? noPassword : passwordToJoinListLobby);
                        }

                        GUILayout.Label($"{lobby.Name ?? LobbyGUILabels.Unnamed} by {GetLobbyHost(lobby)?.Data[PlayerDataForLobby.Username].Value ?? LobbyGUILabels.Unknown}");
                        GUILayout.Label($"Slots: {lobby.Players.Count} / {lobby.MaxPlayers}");

                        GUILayout.EndVertical();
                    }
#if false
                    for (int i = 0; i < 5; i++)
                    {
                        Rect rect = EditorGUILayout.BeginVerticalCachedGUIStylesNames.Box);

                        if (GUI.Button(rect, GUIContent.none))
                        {
                            Debug.Log($"Clicked: {i}");
                        }

                        GUILayout.Label($"Unnamed {i} by Unknown", GUILayout.Width(200));
                        GUILayout.Label($"Slots: {1} / {2}");

                        GUILayout.EndVertical();
                    }
#endif
                    GUILayout.EndScrollView();
                }
            }
            else
            {
                GUILayout.Space(50); // trust
            }

            GUILayout.EndVertical();

            GUILayout.BeginVertical();

            GUILayout.BeginHorizontal(CachedGUIStylesNames.Box);

            GUILayout.Label(LobbyGUILabels.Password, GUILayout.Width(labelWidth));
            passwordToJoinListLobby = GUILayout.TextField(passwordToJoinListLobby, GUILayout.Width(fieldWidth));

            GUILayout.EndHorizontal();

            GUILayout.EndVertical();

            GUILayout.EndVertical();
            GUI.enabled = true;
        }
#nullable disable

        public struct DropdownData
        {
            public bool Expanded;
            public int SelectedIndex;
            public string[] Options;

            public readonly bool Invalid => Options.Length == 0;
            public readonly string Current => Options[SelectedIndex];
        }
        private DropdownData gameModesDropDown = new();
        private DropdownData mapsDropDown = new();
        private Type currentMapDropDownModelType;

        private void UpdateDropdownOptions(Type type, ref DropdownData dropDown)
        {
            dropDown.SelectedIndex = 0;
            
            FieldInfo[] fields = type.GetFields(BindingFlags.Public | BindingFlags.Static);

            dropDown.Options = new string[fields.Length];
            for (int i = 0; i < fields.Length; i++)
            {
                dropDown.Options[i] = (string)fields[i].GetValue(null);
            }
        }

        private void UpdateDropdownOptions(Type type, ref DropdownData dropDown, string[] extraChoices, bool extraChoicesAtTheStart)
        {
            dropDown.SelectedIndex = 0;

            FieldInfo[] fields = type.GetFields(BindingFlags.Public | BindingFlags.Static);

            dropDown.Options = new string[fields.Length + extraChoices.Length];

            int fieldStartIndex = extraChoicesAtTheStart ? extraChoices.Length : 0;
            int extraChoicesStartIndex = extraChoicesAtTheStart ? 0 : fields.Length;

            for (int i = 0; i < extraChoices.Length; i++)
            {
                dropDown.Options[extraChoicesStartIndex + i] = extraChoices[i];
            }

            for (int i = 0; i < fields.Length; i++)
            {
                dropDown.Options[fieldStartIndex + i] = (string)fields[i].GetValue(null);
            }
        }

        private void UpdateDropdownOptions(string[] choices, ref DropdownData dropDown)
        {
            dropDown.SelectedIndex = 0;

            dropDown.Options = new string[choices.Length];
            for (int i = 0; i < choices.Length; i++)
            {
                dropDown.Options[i] = choices[i];
            }
        }

        private void UpdateDropdownOptions(string[] choices, ref DropdownData dropDown, string[] extraChoices, bool extraChoicesAtTheStart)
        {
            dropDown.SelectedIndex = 0;

            dropDown.Options = new string[choices.Length + extraChoices.Length];

            int fieldStartIndex = extraChoicesAtTheStart ? extraChoices.Length : 0;
            int extraChoicesStartIndex = extraChoicesAtTheStart ? 0 : choices.Length;

            for (int i = 0; i < extraChoices.Length; i++)
            {
                dropDown.Options[extraChoicesStartIndex + i] = extraChoices[i];
            }

            for (int i = 0; i < choices.Length; i++)
            {
                dropDown.Options[fieldStartIndex + i] = choices[i];
            }
        }

        private void DropdownMenu(ref DropdownData dropdown)
        {
            GUILayout.BeginVertical(GUILayout.Width(150));

            if (dropdown.Invalid)
            {
                GUILayout.Label("This dropdown is invalid");
                GUILayout.EndVertical();
            }

            GUI.color = dropdown.Expanded ? Color.grey : Color.white;

            if (GUILayout.Button(dropdown.Options[dropdown.SelectedIndex], GUILayout.Height(30)))
            {
                dropdown.Expanded = !dropdown.Expanded;
            }

            GUI.color = Color.white;

            if (dropdown.Expanded)
            {
                for (int index = 0; index < dropdown.Options.Length; index++)
                {
                    if (index == dropdown.SelectedIndex) { continue; }

                    if (GUILayout.Button(dropdown.Options[index], GUILayout.Height(25)))
                    {
                        dropdown.SelectedIndex = index;
                        dropdown.Expanded = false;
                    }
                }
            }

            GUILayout.EndVertical();
        }

        private void OnGUI()
        {
            if (!LobbyMenuActive) { return; }

            UpdateLabelStyles();

            GUILayout.BeginHorizontal(GUILayout.Width(Screen.width)); // H

            GUILayout.FlexibleSpace(); // H-

            GUILayout.BeginVertical(CachedGUIStylesNames.Box); // H-V

            GUILayout.FlexibleSpace(); // H-V-

            GUILayout.BeginHorizontal(); // H-V-H


            if (isSignedIn)
            {
                GUILayout.Label($"Signed in as {ProfileName}");
                GUILayout.EndHorizontal();   // H-V-

                GUILayout.FlexibleSpace();  // H-V

                GUILayout.EndVertical(); // H-

                GUILayout.FlexibleSpace(); // H

                GUILayout.EndHorizontal(); //
            }
            else
            {
                GUILayout.Label(LobbyGUILabels.Nickname, GUILayout.Width(labelWidth));
                ProfileName = GUILayout.TextField(ProfileName, GUILayout.Width(fieldWidth));

                GUILayout.EndHorizontal(); // H-V-

                GUILayout.FlexibleSpace(); // H-V

                GUILayout.BeginHorizontal(); // H-VH

                GUI.enabled = !isSigningIn;
                if (GUILayout.Button(isSigningIn ? LobbyGUILabels.SigningIn : LobbyGUILabels.SignInButtonText) || Input.GetKeyDown(KeyCode.Return) && !isSigningIn)
                {
                    SignIn();
                }
                GUI.enabled = true;
                GUILayout.EndHorizontal(); // H-V

                GUILayout.EndVertical(); // H-

                GUILayout.FlexibleSpace(); // H

                GUILayout.EndHorizontal(); // 
            }


            if (IsInLobby())
            {
                GUILayout.BeginHorizontal(GUILayout.Width(Screen.width));

                GUILayout.FlexibleSpace();

                    if (IsLobbyHost())
                    {
                        GUILayout.BeginVertical(CachedGUIStylesNames.Box);

                        EditLobbyMenu();

                        GUILayout.EndVertical();

                        GUILayout.FlexibleSpace();
                    }                

                    GUILayout.BeginVertical(CachedGUIStylesNames.Box);

                        DisplayCurrentLobbyMenu();

                    GUILayout.EndVertical();

                GUILayout.FlexibleSpace();

                GUILayout.EndHorizontal();
            }
            else
            {
                GUILayout.BeginHorizontal(GUILayout.Width(Screen.width));

                GUILayout.FlexibleSpace();

                    GUILayout.BeginVertical(CachedGUIStylesNames.Box);

                        CreateLobbyMenu();

                        if (!isCreatingLobby)
                        {
                                GUILayout.Space(SpaceBetweenButtons);

                                JoinLobbyMenu();

#if DEV_BUILD
                                GUILayout.Space(SpaceBetweenButtons);

                                LocalTestingMenu();
#endif
                        }

                GUILayout.EndVertical();

                GUILayout.FlexibleSpace();

                    GUILayout.BeginVertical(CachedGUIStylesNames.Box);

                        DisplayAvailableLobbiesMenu();

                    GUILayout.EndVertical();

                GUILayout.FlexibleSpace();

                GUILayout.EndHorizontal();
            }
        }

        public void ToggleLobbyMenu(bool towardOn)
        {
            LobbyMenuActive = towardOn;
        }

        #endregion

        #region Massively hinder GUI s memory muncher abilities

        public static class LobbyGUILabels
        {
            public static readonly string SignInButtonText = "Sign In (required to use any lobby feature)";
            public static readonly string CreateYourLobby = "Create your lobby";
            public static readonly string RequiresSignIn = "(Requires sign in)";
            public static readonly string SigningIn = "(Requires sign in)";
            public static readonly string CreateLobby = "Create lobby";
            public static readonly string EditYourLobby = "Edit your lobby";
            public static readonly string EditLobby = "Edit lobby";
            public static readonly string CloseLobbyAccess = "Close lobby access";
            public static readonly string DeleteLobby = "Delete lobby";
            public static readonly string ShareYourLobbyToAFriend = "Share your lobby to a friend";
            public static readonly string CopyLobbyID = "Copy lobby ID";
            public static readonly string CopyLobbyCode = "Copy lobby code";
            public static readonly string LobbyName = "Lobby Name";
            public static readonly string LobbyCapacity = "Lobby Capacity";
            public static readonly string MakeLobbyPrivate = "Make lobby private";
            public static readonly string LobbyPassword = "Lobby password (Blank or at least 8 caracters)";
            public static readonly char CensoredChar = '*';
            public static readonly string LocalTesting = "Local Testing";
            public static readonly string StartALocalSession = "Start a local session";
            public static readonly string JoinALocalSession = "Join a local session";
            public static readonly string JoinAFriendLobby = "Join a friend lobby";
            public static readonly string TargetLobbyCode = "Target lobby code";
            public static readonly string JoinLobbyByCode = "Join Lobby by code";
            public static readonly string YouMustProvideACode = "You must provide a code";
            public static readonly string TargetLobbyID = "Target lobby ID";
            public static readonly string JoinLobbyByID = "Join lobby by ID";
            public static readonly string YouMustProvideAnID = "You must provide an ID";
            public static readonly string JoinLobbyWithClipboard = "Join Lobby with Clipboard";
            public static readonly string SeemsLikeYourClipboardIsEmpty = "SeemsLikeYourClipboardIsEmpty";
            public static readonly string CurrentLobbyColon = "Current Lobby: ";
            public static readonly string Nickname = "Nickname";
            public static readonly string Password = "Password";
            public static readonly string HostColon = "Host: ";
            public static readonly string PlayersColon = "Players: ";
            public static readonly string CloseLobbyAndStartTeamSelection = "Close lobby and start team selection";
            public static readonly string QuitLobby = "Quit lobby";
            public static readonly string Lobbies = "Lobbies";
            public static readonly string NoLobbyFound = "No lobby found";
            public static readonly string SearchForLobbies = "Search for lobbies";
            public static readonly string Unnamed = "Unnamed";
            public static readonly string Unknown = "Unknown"; 
            public static readonly string SelectAMap = "Select a map"; 
            public static readonly string SelectAGameMode = "Select a gamemode"; 
            public static readonly string GameMode = "Gamemode"; 
            public static readonly string Map = "Map"; 
        }

        #endregion
    }
}

// if not in lobby -> create + join + local + LOBBYLIST
// if in lobby -> (host ? edit + share : quit) + LOBBYDATA

