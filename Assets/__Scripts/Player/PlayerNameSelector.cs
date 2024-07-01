using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class PlayerNameSelector : NetworkBehaviour
{
    [SerializeField] private GameObject playerPrefab;
    private GameObject playerGameObject;

    private string playerName = "";
    private string message = "";
    private bool wasInitialized = false;
    private bool active = true;

    private static GUIStyle labelStyle;
    private static GUIStyle buttonStyle;
    private static GUIStyle textFieldStyle;

    private static readonly int screenWidth = Screen.width;
    private static readonly int screenHeight = Screen.height;

    #region Label

    private static readonly int labelWidth = 200;
    private static readonly int labelHeight = 30;
    private static readonly Rect labelRect = new((screenWidth - labelWidth) / 2, screenHeight / 2 - 60, labelWidth, labelHeight);
    private static readonly string label = "Enter your name:";

    #endregion

    #region Input Field

    private static readonly int inputFieldWidth = 200;
    private static readonly int inputFieldHeight = 30;
    private static readonly Rect inputFieldRect = new((screenWidth - inputFieldWidth) / 2, screenHeight / 2 - 20, inputFieldWidth, inputFieldHeight);

    #endregion

    #region Login Button

    private static readonly int loginButtonWidth = 100;
    private static readonly int loginButtonHeight = 30;
    private static readonly Rect loginButtonRect = new((screenWidth - loginButtonWidth) / 2, screenHeight / 2 + 20, loginButtonWidth, loginButtonHeight);
    private static readonly string loginButtonText = "Login";

    #endregion

    #region Feedback Message

    private static readonly int messageWidth = 300;
    private static readonly int messageHeight = 30;
    private static readonly Rect messageRect = new((screenWidth - messageWidth) / 2, screenHeight / 2 + 60, messageWidth, messageHeight);

    private static readonly string noNameEnteredMessage = "Please enter a name.";

    #endregion



    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        transform.GetChild(0).gameObject.SetActive(IsOwner);
    }

    private void Init()
    {
        wasInitialized = true;

        labelStyle = new(GUI.skin.label)
        {
            fontSize = 20,
            alignment = TextAnchor.MiddleCenter,
        };

        buttonStyle = new(GUI.skin.button)
        {
            fontSize = 20,
        };

        textFieldStyle = new(GUI.skin.textField)
        {
            fontSize = 20,
        };

        active = IsOwner;
    }

    private void OnGUI()
    {
        if (!wasInitialized)
        {
            Init();
        }

        if(!active) { return; }

        GUI.Label(labelRect, label, labelStyle);

        playerName = GUI.TextField(inputFieldRect, playerName, textFieldStyle);

        if (GUI.Button(loginButtonRect, loginButtonText, buttonStyle))
        {
            if (!string.IsNullOrEmpty(playerName))
            {
                CheckWetherNameAvailableServerRpc();
            }
            else
            {
                message = noNameEnteredMessage;
            }
        }

        GUI.Label(messageRect, message, labelStyle);
    }

    [ServerRpc]
    private void CheckWetherNameAvailableServerRpc(ServerRpcParams rpcParams = default)
    {
        if (Game.Manager.PlayerWithNameExist(playerName))
        {
            message = $"Name {playerName} is already in use!";
        }
        else
        {
            RequestSpawnPlayerServerRpc(rpcParams.Receive.SenderClientId);
        }
    }

    //[Rpc(SendTo.Server)]
    //private void RequestSpawnPlayerServerRpc(int paramToAvoidCallingThisOne)
    //{
    //    playerGameObject = Instantiate(playerPrefab, Vector3.up * 5f, Quaternion.identity);
    //    var networkObjectComponent = playerGameObject.GetComponent<NetworkObject>();
    //    networkObjectComponent.SpawnAsPlayerObject(networkObjectComponent.OwnerClientId);
    //    playerGameObject.GetComponent<PlayerFrame>().InitPlayerFrame(playerName);
    //    transform.GetChild(0).gameObject.SetActive(active = false); // deactivate camera
    //    // may cause issues when there are several clients (when spawning a new player) ? idk what I was thinking about but may be true tho
        
    //    //InitSpawnedPlayerClientRpc();
    //}

    [Rpc(SendTo.Server)]
    private void RequestSpawnPlayerServerRpc(ulong senderClientID)
    {
        playerGameObject = Instantiate(playerPrefab, Vector3.up * 5f, Quaternion.identity);

        playerGameObject.GetComponent<NetworkObject>().SpawnAsPlayerObject(senderClientID);

        // Initialize the player frame (assuming InitPlayerFrame handles setting up the player name, etc.)
        playerGameObject.GetComponent<PlayerFrame>().InitPlayerFrame(playerName);

        active = false;
        transform.GetChild(0).gameObject.SetActive(false);
    }

        [Rpc(SendTo.ClientsAndHost)]
    private void InitSpawnedPlayerClientRpc()
    {
        print("Tried spawning player on this client");
        StartCoroutine(InitPlayerFrame());
    }

    private IEnumerator InitPlayerFrame()
    {
        yield return new WaitUntil(() => playerGameObject != null);
    }
}
