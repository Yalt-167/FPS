using JetBrains.Annotations;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using UnityEngine.PlayerLoop;

public class LobbyHandler : MonoBehaviour
{
    public int SpaceBetweenButtons = 20;
    public Lobby hostLobby;
    private readonly float heartbeat = 15f; // what pings the lobby for it to stay active when not interacted with (in seconds)
    private float heartbeatTimer; // what pings the lobby for it to stay active when not interacted with (in seconds)

    #region Filters Handling

    public FiltersValuesStruct FiltersValues;
    public FiltersStruct Filters;

    private void InitializeFilters()
    {
        FiltersValues = new()
        {
            GameModeFilter = DataObject.IndexOptions.S1,
            HasPassword = DataObject.IndexOptions.S2,
        };

        Filters = new()
        {
            GameModeFilter = QueryFilter.FieldOptions.S1,
            HasPassword = QueryFilter.FieldOptions.S2,
        };

    }
    public struct FiltersValuesStruct
    {
        public DataObject.IndexOptions GameModeFilter;
        public DataObject.IndexOptions HasPassword;
    }

    public struct FiltersStruct
    {
        public QueryFilter.FieldOptions GameModeFilter;
        public QueryFilter.FieldOptions HasPassword;
    }

    #endregion

    private async void Start()
    {
        InitializeFilters();

        await UnityServices.InitializeAsync();

        AuthenticationService.Instance.SignedIn += SignInCallback;

        await AuthenticationService.Instance.SignInAnonymouslyAsync();
    }

    

    private void Update()
    {
        HandleHeartbeat();
    }

    private async void HandleHeartbeat()
    {
        if (hostLobby != null) {  return; }

        heartbeatTimer += Time.deltaTime;
        if (heartbeatTimer >= heartbeat)
        {
            await LobbyService.Instance.SendHeartbeatPingAsync(hostLobby.Id);
            heartbeatTimer = 0f;
        }
    }

    public void SignInCallback()
    {
        Debug.Log($"Signed in as {AuthenticationService.Instance.PlayerId}");
    }

#pragma warning disable

    public string LobbyName;
    public int LobbyCapacity;

    public bool PrivateLobby; // Private lobbies are NEVER visible in query results and require the lobby CODE or ID to be manually provided to new players.
    public string Password;

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

        var lobbyOptions = new CreateLobbyOptions()
        {
            IsPrivate = true,
            Player = GetPlayer(),
            Data = new Dictionary<string, DataObject>()
            {
                {
                    "GameMode", 
                    new DataObject(
                        visibility: DataObject.VisibilityOptions.Public,
                        value: "Deathmatch",
                        index: FiltersValues.GameModeFilter // GameModeFilter value being S1 -> it s now linked to the QueryFilter.FieldOptions.S1
                    )
                },

                {
                    "HasPassword",
                    new DataObject(
                        visibility: DataObject.VisibilityOptions.Public,
                        value: "Deathmatch",
                        index: FiltersValues.HasPassword // GameModeFilter value being S1 -> it s now linked to the QueryFilter.FieldOptions.S1
                    )

                }
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

        Debug.Log($"Successfully created a new lobby named {hostLobby.Name} with {hostLobby.MaxPlayers} slots");
    }

    public string EditLobbyParamString;
    public async void EditLobby()
    {
        throw new System.NotImplementedException();
    }

    public string DeleteLobbyParamString;
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


    public string JoinLobbyByIDParamString;
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

    public string JoinLobbyByCodeParamString;
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

    public string ListLobbiesParamString;
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
                    new QueryFilter(QueryFilter.FieldOptions.Name, "that ll do for now", QueryFilter.OpOptions.EQ),
                    new QueryFilter(QueryFilter.FieldOptions.AvailableSlots, "5", QueryFilter.OpOptions.LT),
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
