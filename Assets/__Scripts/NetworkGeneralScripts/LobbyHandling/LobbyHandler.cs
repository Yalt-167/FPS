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
    public static LobbyHandler DebugInstance;

    public Lobby hostLobby;
    private readonly float heartbeat = 15f; // what pings the lobby for it to stay active when not interacted with (in seconds)
    private float heartbeatTimer; // what pings the lobby for it to stay active when not interacted with (in seconds)

    private async void Start()
    {
        DebugInstance = this;

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


    public static void SignInCallback()
    {
        Debug.Log($"Signed in as {AuthenticationService.Instance.PlayerId}");
    }

#pragma warning disable

    [MenuItem("Developer/Lobby/Create")]
    public static async void CreateLobby()
    {
        var lobbyName = "that ll do for now";
        var lobbyCapacity = 4;
        var lobbyOptions = new CreateLobbyOptions();

        try
        {
            DebugInstance.hostLobby = await LobbyService.Instance.CreateLobbyAsync(lobbyName, lobbyCapacity, lobbyOptions);
        }
        catch (LobbyServiceException exception)
        {
            Debug.Log($"Couldn t create a lobby. Reason: {exception.Message}");
            return;
        }

        Debug.Log($"Successfully created a new lobby named {DebugInstance.hostLobby.Name} with {DebugInstance.hostLobby.MaxPlayers} slots");
    }

    [MenuItem("Developer/Lobby/Edit")]
    public static async void EditLobby()
    {
        throw new System.NotImplementedException();
    }

    [MenuItem("Developer/Lobby/Delete")]
    public static async void DeleteLobby()
    {
        throw new System.NotImplementedException();
    }

    [MenuItem("Developer/Lobby/Join")]
    public static async void JoinLobby()// so far only joinds the first lobby
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
        }

        Debug.Log("Succesufully joined lobby");
    }

    [MenuItem("Developer/Lobby/List")]
    public static async void ListLobbies()
    {
        try
        {
            //QueryLobbiesOptions options = new QueryLobbiesOptions()
            //{
            //    Count = 25,
            //    Filters = new List<QueryFilter>()
            //    {
            //        new QueryFilter(QueryFilter.FieldOptions.Name, "testLobby", QueryFilter.OpOptions.EQ),
            //        new QueryFilter(QueryFilter.FieldOptions.AvailableSlots, "5", QueryFilter.OpOptions.LT),
            //    },
            //    Order = new List<QueryOrder>()
            //    {
            //        new QueryOrder(true, QueryOrder.FieldOptions.Name),
            //    }

            //};

            //QueryResponse response = await Lobbies.Instance.QueryLobbiesAsync(options);

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
        }
    }


    public static async Task<QueryResponse>? EnumerateLobbiesAsync()
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
                    new QueryOrder(true, QueryOrder.FieldOptions.Name),
                }

            };

            QueryResponse response = await Lobbies.Instance.QueryLobbiesAsync(options);

            return response;
        }
        catch (LobbyServiceException exception)
        {
            Debug.Log(exception.Message);
        }

        return null;
    }
#pragma warning enable


}
