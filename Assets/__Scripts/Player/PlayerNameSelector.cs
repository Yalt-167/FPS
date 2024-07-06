#define PLAYER_PACK_ARCHITECTURE

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using Unity.Multiplayer.Samples.Utilities.ClientAuthority;


public class PlayerNameSelector : NetworkBehaviour
{
    [SerializeField] private GameObject playerPrefab;
    private GameObject playerGameObject;

    private NetworkSerializableString netPlayerName = new();

    public NetworkSerializableString netErrorMessage = new();
    private bool wasInitialized = false;
    private bool promptActive = true;

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

        promptActive = IsOwner;
}

    private void OnGUI()
    {
        if (!wasInitialized)
        {
            Init();
        }

        if(!promptActive) { return; }

        GUI.Label(labelRect, label, labelStyle);

        netPlayerName.Value = GUI.TextField(inputFieldRect, netPlayerName, textFieldStyle);

        if (GUI.Button(loginButtonRect, loginButtonText, buttonStyle))
        {
            if (!string.IsNullOrEmpty(netPlayerName))
            {
                CheckWetherNameAvailableServerRpc(netPlayerName);
            }
            else
            {
                netErrorMessage.Value = noNameEnteredMessage;
            }
        }

        GUI.Label(messageRect, netErrorMessage.Value, labelStyle);
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void SetErrorMessageClientRpc(NetworkSerializableString message)
    {
        netErrorMessage.Value = message;
    }

    [ServerRpc]
    private void CheckWetherNameAvailableServerRpc(NetworkSerializableString playerName, ServerRpcParams rpcParams = default)
    {
        if (Game.Manager.PlayerWithNameExist(playerName))
        {
            SetErrorMessageClientRpc(new($"Name {playerName} is already in use!"));
        }
        else
        {

# if PLAYER_PACK_ARCHITECTURE
            EnablePlayerServerRpc(playerName);
#else
            //RequestSpawnPlayerServerRpc(playerName, rpcParams.Receive.SenderClientId);
#endif
        }
    }

    [Rpc(SendTo.Server)]
    private void EnablePlayerServerRpc(NetworkSerializableString playerName)
    {
        ActivatePlayerPackArchitectureClientRpc(playerName);

        //DeactivateLoginPromptClientRpc();
        //DeactivateLoginCameraClientRpc();
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
    private void RequestSpawnPlayerServerRpc(NetworkSerializableString playerName, ulong senderClientID)
    {
        SpawnPlayerClientRpc();
        playerGameObject.GetComponent<NetworkObject>().SpawnAsPlayerObject(senderClientID);
        //ActivatePlayerPackArchitectureClientRpc(playerName);
        //playerGameObject.GetComponent<PlayerFrame>().InitPlayerFrame(playerName);

        DeactivateLoginPromptClientRpc();
        DeactivateLoginCameraClientRpc();
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void SpawnPlayerClientRpc()
    {
        playerGameObject = Instantiate(playerPrefab, Vector3.up * 5f, Quaternion.identity);
    }
    
    
    [Rpc(SendTo.ClientsAndHost)]
    private void ActivatePlayerPackArchitectureClientRpc(NetworkSerializableString playerName)
    {
        GetComponent<PlayerMovement>().enabled = true;
        GetComponent<PlayerCombat>().enabled = true;
        GetComponent<ClientNetworkTransform>().enabled = true;
        GetComponent<PlayerHealthDisplay>().enabled = true;
        GetComponent<WeaponHandler>().enabled = true;
        GetComponent<PlayerHealthNetworked>().enabled = true;

        GetComponent<PlayerFrame>().InitPlayerFrame(playerName);

        for (int childTransformIndex = 0; childTransformIndex < 4; childTransformIndex++)
        {
            transform.GetChild(childTransformIndex).gameObject.SetActive(true); // every subGameObject of the player
        }
        transform.GetChild(5).gameObject.SetActive(false); // Main Camera
        enabled = false;
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void ActivatePlayerPlayerLoginArchitectureClientRpc(NetworkSerializableString playerName)
    {
        playerGameObject.GetComponent<PlayerFrame>().InitPlayerFrame(playerName);
    }


    [Rpc(SendTo.ClientsAndHost)]
    private void DeactivateLoginPromptClientRpc()
    {
        promptActive = false;
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void DeactivateLoginCameraClientRpc()
    {
        transform.GetChild(0).gameObject.SetActive(false);
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
    public override string ToString()
    {
        return Value;
    }
}

//public struct NetworkSerializableData<T> : INetworkSerializable 
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


// make the player prefab a child of the login instead and use its NetworkObject