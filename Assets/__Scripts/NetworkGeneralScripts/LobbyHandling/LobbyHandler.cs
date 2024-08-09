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

namespace LobbyHandling
{
    public sealed class LobbyHandler : MonoBehaviour
    {
        public int SpaceBetweenButtons = 12;
        private Lobby hostLobby;
        private static readonly float heartbeat = 15f; // what pings the lobby for it to stay active when not interacted with (in seconds)
        private float heartbeatTimer;
        private static readonly string noPassword = "        ";

        public string ProfileName;
        private Player localPlayer;

        #region Filters Handling

        public FiltersValuesStruct FiltersValues;
        public FiltersStruct Filters;

        private void InitializeFilters()
        {
            FiltersValues = new()
            {
                GameMode = DataObject.IndexOptions.S1,
                HasPassword = DataObject.IndexOptions.S2,
                IsRanked = DataObject.IndexOptions.S3,
            };

            Filters = new()
            {
                GameMode = QueryFilter.FieldOptions.S1,
                HasPassword = QueryFilter.FieldOptions.S2,
                IsRanked = QueryFilter.FieldOptions.S3,
            };

        }

        public struct FiltersValuesStruct
        {
            public DataObject.IndexOptions GameMode;
            public DataObject.IndexOptions HasPassword;
            public DataObject.IndexOptions IsRanked;
        }

        public struct FiltersStruct
        {
            public QueryFilter.FieldOptions GameMode;
            public QueryFilter.FieldOptions HasPassword;
            public QueryFilter.FieldOptions IsRanked;
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

        }

        public async void SignInAsync(string sighInAs)
        {
            AuthenticationService.Instance.SwitchProfile(sighInAs);

            await AuthenticationService.Instance.SignInAnonymouslyAsync();

            localPlayer = GetPlayer();
        }

        #region Callbacks

        private void SignInCallback()
        {
            Debug.Log($"Signed in as {AuthenticationService.Instance.PlayerId}");
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
        }

        private async void HandleHeartbeat()
        {
            if (hostLobby == null) { return; }

            heartbeatTimer += Time.deltaTime;
            if (heartbeatTimer >= heartbeat)
            {
                heartbeatTimer = 0f;
                if (hostLobby == null) { return; }
                await LobbyService.Instance.SendHeartbeatPingAsync(hostLobby.Id);
            }
        }

        #endregion


        public string LobbyName;
        public int LobbyCapacity;

        public bool PrivateLobby; // Private lobbies are NEVER visible in query results and require the lobby CODE or ID to be manually provided to new players.
        public string Password;
        public string TargetLobbyCode;
        public string TargetLobbyId;

        public async void CreateLobby(string lobbyName, int lobbyCapacity, bool privateLobby, string password)
        {
            var emptyPassword = string.IsNullOrEmpty(password);
            if (!emptyPassword && password.Length < 8)
            {
                Debug.Log("Your lobby was not created as your password doesn t have enough characters (at least 8)");
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
                            index: FiltersValues.GameMode // GameModeFilter value being S1 -> it s now linked to the QueryFilter.FieldOptions.S1
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
            CopyLobbyID();

        }

        public async void EditLobby(string lobbyID, string lobbyName, int lobbyCapacity, bool privateLobby, string password)
        {
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
        }

        public async void JoinLobbyByID(string lobbyID, string password)
        {
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
        }

        public async void JoinLobbyByCode(string lobbyCode, string password)
        {
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
        }

        public async void QuitLobby()
        {
            await KickPlayer(AuthenticationService.Instance.PlayerId);
        }

        public async Task KickPlayer(string playerID)
        {
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

        public async void SetHost(string newHostPlayerID)
        {
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
                Debug.Log($"{player.Data[LobbyPlayerData.Username].Value}");
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

        #endregion

        private Player GetPlayer()
        {
            return new Player(id: AuthenticationService.Instance.PlayerId)
            {
                Data = new Dictionary<string, PlayerDataObject>()
                {
                    { LobbyPlayerData.Username, new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member, ProfileName) }
                }
            };
        }
    }
}

