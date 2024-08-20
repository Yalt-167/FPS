//#define HEADLESS_ARCHITECTURE_SERVER
#define LOG_LOBBY_EVENTS

using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;

using UnityEditor;
using UnityEngine;

using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using Unity.Netcode.Transports.UTP;
using Unity.Netcode;
using Unity.Services.Relay.Models;
using Unity.Services.Relay;
using AYellowpaper.SerializedCollections.Editor;
using GameManagement;

namespace LobbyHandling
{
    public sealed class LobbyHandler : MonoBehaviour
    {
        public int SpaceBetweenButtons = 12;

        private Lobby hostLobby;

        private static readonly float heartbeat = 15f; // what pings the lobby for it to stay active when not interacted with (in seconds)
        private float heartbeatTimer;

        private static readonly float lobbyUpdateRate = 300f; // how often the lobby updates (in seconds)
        private float lobbyUpdateTimer;

        private static readonly string noPassword = "        ";
        private static readonly string initializeToZero = "0";

        public string ProfileName;
        private bool isSignedIn;
        private Player localPlayer;

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
            InitializeFilters();

            await UnityServices.InitializeAsync();

            AuthenticationService.Instance.SignedIn += SignInCallback;
            AuthenticationService.Instance.SignInFailed += SignInFailedCallback;

            AuthenticationService.Instance.SignedOut += SignOutCallback;
            AuthenticationService.Instance.Expired += SessionExpiredCallback;

            InitializeUnityTransports();

            InitLabelStyles();
            lobbyMenuActive = true;
        }

        public async void SignInAsync()
        {
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
        }

        private void SessionExpiredCallback()
        {
            Debug.Log("Session Expired");
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
            //HandleLobbyUpdate();
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

            lobbyUpdateTimer += Time.deltaTime;
            if (lobbyUpdateTimer >= lobbyUpdateRate)
            {
                lobbyUpdateTimer = 0f;

                if (hostLobby == null) { return; }

                hostLobby = await LobbyService.Instance.GetLobbyAsync(hostLobby.Id);
            }
        }

        #endregion

        #region Lobby Actions

        [Header("lobby settings")]
        public string LobbyName;
        public int LobbyCapacity;

        public bool PrivateLobby; // Private lobbies are NEVER visible in query results and require the lobby CODE or ID to be manually provided to new players.
        public string Password;

        [Header("Target lobby")]
        public string TargetLobbyCode;
        public string TargetLobbyID;

        [Header("Relay")]
        public string RelayJoinCode;

        public async void CreateLobby(string lobbyName, int lobbyCapacity, bool privateLobby, string password)
        {
            var emptyPassword = string.IsNullOrEmpty(password);
            if (!emptyPassword && password.Length < 8)
            {
                Debug.Log("Your lobby was not created as your password doesn t have enough characters (at least 8)");
                return;
            }

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
                            value: GameModes.DeathMatch,
                            index: FiltersValues.GameMode
                        )
                    },

                    {
                        LobbyData.Map,
                        new DataObject(
                            visibility: DataObject.VisibilityOptions.Public,
                            value: Maps.ToBeVoted,
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
                return;
            }

            Debug.Log($"Successfully created a new lobby");
            DisplayHostLobbyData();
            FillInLobbyDataFields();
            CopyLobbyID();

        }

        public async void EditLobby(string lobbyID, string lobbyName, int lobbyCapacity, bool privateLobby, string password)
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
                Password = emptyPassword ? noPassword : password

                //Data = new Dictionary<string, DataObject>() // idk how to represent that as param so far
                //{

                //}
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

            Debug.Log($"Successfully edited lobby data");
            DisplayHostLobbyData();
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

            Debug.Log("Lobby was successfully deleted");
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

            Debug.Log($"Succesfully joined lobby: {hostLobby.Name}");

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

            Debug.Log($"Succesfully joined lobby: {hostLobby.Name}");

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

            Debug.Log($"Succesfully joined lobby: {hostLobby.Name}");

            JoinRelay(hostLobby.Data[LobbyData.RelayJoinCode].Value);
        }

        public async void QuitLobby()
        {
            await QuitLobbyAsync(AuthenticationService.Instance.PlayerId);
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
                await LobbyService.Instance.UpdateLobbyAsync(hostLobby.Id, updateLobbyOptions);
            }
            catch (LobbyServiceException exception)
            {
                Debug.Log(exception.Message);
                return;
            }


            Debug.Log("Suucessfully closed the lobby");
        }

        public async void KickPlayer(string playerID)
        {
            if (!IsLobbyHost())
            {
                Debug.Log("You don t have permission for this");
                return;
            }

            await QuitLobbyAsync(playerID);
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

            Debug.Log($"Successfully set new host");
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

        private void FillInLobbyDataFields()
        {
            if (hostLobby == null) { return; };

            TargetLobbyCode = hostLobby.LobbyCode;
            TargetLobbyID = hostLobby.Id;
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

            Debug.Log("Successfully created relay");

            bool success =
#if HEADLESS_ARCHITECTURE_SERVER
            NetworkManager.Singleton.StartServer()
#else
            NetworkManager.Singleton.StartHost()
#endif
                ;

            Debug.Log(success ? "Server was successfully started" : "Failed to start server");
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

            Debug.Log("Successfully joined relay");
        }

        public async void ListRegions()
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
                //Debug.Log(region.)
            }
        }

        #endregion

        #region Debug

        public async void DisplayLobbies()
        {
            try
            {
                var lobbies = await EnumerateLobbiesAsync();

                if (lobbies == null) { return; }

                Debug.Log($"Found {lobbies.Results.Count} lobb{(lobbies.Results.Count == 1 ? "y" : "ies")} matching your criteria, namely:");

                foreach (var lobby in lobbies.Results)
                {
                    DisplayLobbyData(lobby);
                }
            }
            catch (LobbyServiceException exception)
            {
                Debug.Log(exception.Message);
                return;
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
            Debug.Log("Lobby ID was copied to your clipboard");
        }

        public void CopyLobbyCode()
        {
            if (hostLobby == null) { return; }

            GUIUtility.systemCopyBuffer = hostLobby.LobbyCode;
            Debug.Log("Lobby code was copied to your clipboard");
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

        #endregion

        private Player GetPlayer()
        {
            return new Player(id: AuthenticationService.Instance.PlayerId)
            {
                Data = new Dictionary<string, PlayerDataObject>()
                {
                    { PlayerDataForLobby.Username, new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member, ProfileName) },
                    // perhaps don t update that and just update it at the end of the game to save on lobby update
                    { PlayerDataForLobby.Kills, new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member, initializeToZero) }, 
                    { PlayerDataForLobby.Deaths, new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member, initializeToZero)},
                    { PlayerDataForLobby.Assists, new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member, initializeToZero) },
                    { PlayerDataForLobby.Damage, new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member, initializeToZero) },
                    { PlayerDataForLobby.Accuracy, new PlayerDataObject(PlayerDataObject.VisibilityOptions.Private, initializeToZero) }
                }
            };
        }

        #region GUI

        [Header("Lobby GUI Settings")]
        [SerializeField] private bool lobbyMenuActive;
        [SerializeField] private int labelWidth;
        [SerializeField] private int fieldWidth;
        private GUIStyle titleLabelStyle;

        private void InitLabelStyles()
        {
            
        }

        private void OnGUI()
        {
            if (!lobbyMenuActive) { return; }

            titleLabelStyle = new GUIStyle(GUI.skin.label)
            {
                fontStyle = FontStyle.Bold,
                fontSize = 24,
            };

            #region Sign In Button
            if (!isSignedIn)
            {
                #region Profile Name Prompt
                GUILayout.BeginHorizontal();
                GUILayout.Label("Nickname", GUILayout.Width(labelWidth));
                ProfileName = GUILayout.TextField(ProfileName, GUILayout.Width(fieldWidth));
                GUILayout.EndHorizontal();
                #endregion

                GUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("     Sign In (required to use any lobby feature)     "))
                {
                    SignInAsync();
                }
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();
            }
            else
            {
                GUILayout.Label($"Signed in as {ProfileName}", GUILayout.Width(labelWidth));
            }

            #endregion

            GUILayout.BeginHorizontal();

                    GUILayout.BeginVertical("box");

                        GUILayout.Label(IsLobbyHost() ? "Edit your lobby" : "Create your lobby", titleLabelStyle);

                        #region Lobby Name Prompt
                        GUILayout.BeginHorizontal();
                        GUILayout.Label("Lobby Name", GUILayout.Width(labelWidth));
                        LobbyName = GUILayout.TextField(LobbyName, GUILayout.Width(fieldWidth));
                        GUILayout.EndHorizontal();
                        #endregion

                        #region Lobby Capacity Prompt
                        GUILayout.BeginHorizontal();
                        GUILayout.Label("Lobby Capacity", GUILayout.Width(labelWidth));
                        LobbyCapacity = int.Parse(GUILayout.TextField(LobbyCapacity.ToString(), GUILayout.Width(fieldWidth)));
                        GUILayout.EndHorizontal();
                        #endregion

                        #region Lobby Privacy Prompt
                        GUILayout.BeginHorizontal();
                        PrivateLobby = GUILayout.Toggle(PrivateLobby, "Private Lobby");
                        GUILayout.EndHorizontal();
                        #endregion

                        #region Lobby Password Prompt
                        GUILayout.BeginHorizontal();
                        GUILayout.Label("Lobby Password (Blank or at least 8 caracters)", GUILayout.Width(labelWidth));
                        Password = GUILayout.PasswordField(Password, '*', GUILayout.Width(fieldWidth));
                        GUILayout.EndHorizontal();
                        #endregion


                        if (IsLobbyHost())
                        {
                            if (GUILayout.Button("Edit Lobby"))
                            {
                                EditLobby(hostLobby.Id, LobbyName, LobbyCapacity, PrivateLobby, Password);
                            }

                            GUILayout.Space(SpaceBetweenButtons);

                            if (GUILayout.Button("Close Lobby Access"))
                            {
                                CloseLobbyAcess();
                            }

                            GUILayout.Space(SpaceBetweenButtons);

                            if (GUILayout.Button("Delete Lobby"))
                            {
                                DeleteLobby(hostLobby.Id);
                            }

                            GUILayout.Space(SpaceBetweenButtons * 2);

                            GUILayout.Label("Share your lobby to a friend", titleLabelStyle);
                            if (GUILayout.Button("Copy Lobby ID"))
                            {
                                CopyLobbyID();
                            }

                            if (GUILayout.Button("Copy Lobby Code"))
                            {
                                CopyLobbyCode();
                            }
                        }
                        else
                        {
                            if (GUILayout.Button("Create Lobby"))
                            {
                                CreateLobby(LobbyName, LobbyCapacity, PrivateLobby, Password);
                            }
                        }

                GUILayout.EndVertical();
                

            GUILayout.BeginVertical("box");

            if (IsInLobby())
            {
                if (GUILayout.Button("Quit Current Lobby"))
                {
                    QuitLobby();
                }
            }
            else
            {
                GUILayout.BeginVertical("box");

                GUILayout.Label("Join a friend lobby", GUILayout.Width(labelWidth));

                #region Target Lobby Code Prompt
                GUILayout.BeginHorizontal();
                GUILayout.Label("Target lobby Code", GUILayout.Width(labelWidth));
                TargetLobbyCode = GUILayout.TextField(TargetLobbyCode, GUILayout.Width(fieldWidth));
                GUILayout.EndHorizontal();
                if (GUILayout.Button("Join Lobby by Code"))
                {
                    if (string.IsNullOrEmpty(TargetLobbyCode))
                    {
                        Debug.Log("You must provide the lobby code");
                    }
                    else
                    {
                        JoinLobbyByCode(TargetLobbyCode, Password);
                    }
                }
                #endregion

                GUILayout.Space(SpaceBetweenButtons);

                #region Target Lobby ID Prompt
                GUILayout.BeginHorizontal();
                GUILayout.Label("Target lobby ID", GUILayout.Width(labelWidth));
                TargetLobbyID = GUILayout.TextField(TargetLobbyID, GUILayout.Width(fieldWidth));
                GUILayout.EndHorizontal();
                if (GUILayout.Button("Join Lobby by ID"))
                {
                    if (string.IsNullOrEmpty(TargetLobbyID))
                    {
                        Debug.Log("You must provide the lobby ID");
                    }
                    else
                    {
                        JoinLobbyByID(TargetLobbyID, Password);
                    }
                }
                #endregion

                GUILayout.Space(SpaceBetweenButtons);

                if (GUILayout.Button("Join Lobby with Clipboard"))
                {
                    JoinLobbyByID(GUIUtility.systemCopyBuffer, Password);
                }

                GUILayout.EndVertical();
            }
            

            if (GUILayout.Button("List Lobbies"))
            {
                DisplayLobbies();
            }

            if (GUILayout.Button("Display Current Lobby Data"))
            {
                DisplayHostLobbyData();
            }

            if (GUILayout.Button("Display Lobby Data by ID"))
            {
                DisplayLobbyData(TargetLobbyID);
            }

            if (GUILayout.Button("Display Current Lobby Players"))
            {
                DisplayHostLobbyPlayers();
            }

            if (GUILayout.Button("Display Lobby Players by ID"))
            {
                DisplayPlayers(TargetLobbyID);
            }

            GUILayout.Space(SpaceBetweenButtons);

            GUILayout.Label("Relay", GUILayout.Width(labelWidth));

            if (GUILayout.Button("List Regions"))
            {
                ListRegions();
            }
            GUILayout.Space(SpaceBetweenButtons);


            #region Local testing
            GUILayout.BeginVertical();

            GUILayout.Label("Local Testing", titleLabelStyle);

            if (GUILayout.Button("Start a Local Session"))
            {
                SelectLocalUnityTransport();
                GameNetworkManager.Singleton.StartHost();
            }

            if (GUILayout.Button("Join a Local Session"))
            {
                SelectLocalUnityTransport();
                GameNetworkManager.Singleton.StartClient();
            }

            GUILayout.EndVertical();
            #endregion

            GUILayout.EndVertical();

            GUILayout.EndHorizontal();
        }


        #endregion
    }
}

