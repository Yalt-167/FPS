using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using UnityEditor;
using UnityEngine;
namespace LobbyHandling
{
    public class LobbyHandler : MonoBehaviour
    {
        public int SpaceBetweenButtons = 12;
        public Lobby hostLobby = null;
        private readonly float heartbeat = 15f; // what pings the lobby for it to stay active when not interacted with (in seconds)
        private float heartbeatTimer; // what pings the lobby for it to stay active when not interacted with (in seconds)
        private readonly string noPassword = "        ";

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

            await AuthenticationService.Instance.SignInAnonymouslyAsync();
        }

        public void SignInCallback()
        {
            Debug.Log($"Signed in as {AuthenticationService.Instance.PlayerId}");
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
                await LobbyService.Instance.SendHeartbeatPingAsync(hostLobby.Id);
                Debug.Log("Heartbeat triggered");
            }
        }

        #endregion

#pragma warning disable

        public string LobbyName;
        public int LobbyCapacity;

        public bool PrivateLobby; // Private lobbies are NEVER visible in query results and require the lobby CODE or ID to be manually provided to new players.
        public string Password;
        public string TargetLobbyCode;
        public string TargetLobbyId;

        public async void CreateLobby(string lobbyName, int lobbyCapacity, bool privateLobby, string password)
        {

#if false

public class CreateLobbyOptions
{
    public bool? IsPrivate { get; set; }

    public string Password { get; set; }

    public bool? IsLocked { get; set; } -> wether people can join so false upon start but lock it when the game begins

    public Player Player { get; set; }

    public Dictionary<string, DataObject> Data { get; set; }
}
#endif

            var emptyPassword = string.IsNullOrEmpty(password);
            if (!emptyPassword && password.Length < 8)
            {
                Debug.Log("Your lobby was not created as your password doesn t have enough characters (at least 8)");
                return;
            }

            var lobbyOptions = new CreateLobbyOptions()
            {
                IsPrivate = privateLobby,
                Player = GetPlayer(),
                Password = emptyPassword ? noPassword : password,
                Data = new Dictionary<string, DataObject>()
            {
                {
                    "GameMode",
                    new DataObject(
                        visibility: DataObject.VisibilityOptions.Public,
                        value: "Deathmatch",
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

            UpdateLobbyOptions updateOptions = new UpdateLobbyOptions()
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

            Debug.Log($"Succesfully edited lobby data");
            DisplayHostLobbyData();
        }

        public async void DeleteLobby()
        {
            var lobbies = await EnumerateLobbiesAsync();

            if (lobbies == null) { return; }

            try
            {
                await LobbyService.Instance.DeleteLobbyAsync(lobbies.Results[0].Id);
            }
            catch (LobbyServiceException exception)
            {
                Debug.Log(exception.Message);
                return;
            }

            Debug.Log("Lobby was succesfully deleted");
        }

        public async void JoinLobbyByID()// so far only joins the first lobby
        {
            var lobby = await EnumerateLobbiesAsync();

            if (lobby == null) { return; }

            try
            {
                await LobbyService.Instance.JoinLobbyByIdAsync(lobby.Results[0].Id);
            }
            catch (LobbyServiceException exception)
            {
                Debug.Log(exception.Message);
                return;
            }

            Debug.Log("Succesufully joined lobby");
        }

        public async void JoinLobbyByCode()// so far only joins the first lobby
        {
            var lobby = await EnumerateLobbiesAsync();

            if (lobby == null) { return; }

            try
            {
                await LobbyService.Instance.JoinLobbyByCodeAsync(lobby.Results[0].LobbyCode);
            }
            catch (LobbyServiceException exception)
            {
                Debug.Log(exception.Message);
            }

            Debug.Log("Succesufully joined lobby");
        }

        public async void ListLobbies()
        {
            try
            {
                var lobbies = await EnumerateLobbiesAsync();

                if (lobbies == null) { return; }

                Debug.Log($"Found {lobbies.Results.Count} lobbies matching your criteria, namely:");

                foreach (var lobby in lobbies.Results)
                {
                    Debug.Log($"{lobby.Name} ({lobby.MaxPlayers})");
                }
            }
            catch (LobbyServiceException exception)
            {
                Debug.Log(exception.Message);
                return;
            }
        }

        public async Task<QueryResponse?> EnumerateLobbiesAsync()
        {
            try
            {
                QueryLobbiesOptions options = new QueryLobbiesOptions()
                {
                    Count = 25,
                    Filters = new List<QueryFilter>()
                    {
                        //new QueryFilter(QueryFilter.FieldOptions.Name, "that ll do for now", QueryFilter.OpOptions.EQ),
                        //new QueryFilter(QueryFilter.FieldOptions.AvailableSlots, "5", QueryFilter.OpOptions.LT),
                    },
                    Order = new List<QueryOrder>()
                {
                    new QueryOrder(true, QueryOrder.FieldOptions.AvailableSlots),
                }
                };

                QueryResponse response = await Lobbies.Instance.QueryLobbiesAsync(options);

                return response.Results.Count > 0 ? response : null;
            }
            catch (LobbyServiceException exception)
            {
                Debug.Log(exception.Message);
            }

            return null;
        }

#pragma warning enable

        private void DisplayHostLobbyData()
        {
            if (hostLobby == null) { return; }

            Debug.Log($"Name: {hostLobby.Name} | Capacity: {hostLobby.MaxPlayers}, Private: {hostLobby.IsPrivate}");
            Debug.Log($"ID: {hostLobby.Id} | Code: {hostLobby.LobbyCode}");


            Debug.Log("Special data");
            var lobbyData = hostLobby.Data;
            foreach (KeyValuePair<string, DataObject> kvp in lobbyData)
            {
                Debug.Log($"{kvp.Key}: {kvp.Value.Value}");
            }
        }

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

        private Player GetPlayer()
        {
            return new Player(id: AuthenticationService.Instance.PlayerId)
            {
                Data = new Dictionary<string, PlayerDataObject>()
            {
                {"Name", new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member, "playerName") }
            }
            };
        }
    }
}

