using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using UnityEngine.UI;

public class PlayerNameSelector : NetworkBehaviour
{
    private string playerName = "";
    private string message = "";
    private bool wasInitialized = false;

    GUIStyle labelStyle;
    GUIStyle buttonStyle;
    GUIStyle textFieldStyle;

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

        if (!IsOwner)
        {
            print("Destroyed parasite Awake");
            Destroy(this);
        }
    }

    private void OnGUI()
    {

        if (!wasInitialized)
        {
            Init();
        }

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

        GUI.Label(
            messageRect,
            message,
            labelStyle
            );
    }

    [Rpc(SendTo.Server)]
    private void CheckWetherNameAvailableServerRpc()
    {
        if (Game.Manager.PlayerWithNameExist(playerName))
        {
            message = $"Name {playerName} is already in use!";
        }
        else
        {
            ContinueOntoTeamSelector();
        }
    }

    private void ContinueOntoTeamSelector()
    {
        print("been there");
        var teamSelectorComponent = gameObject.AddComponent<TeamSelector>();
        teamSelectorComponent.PlayerName = playerName;
        Destroy(this);
    }

    public IEnumerator ChooseNickname()
    {

    }
}
