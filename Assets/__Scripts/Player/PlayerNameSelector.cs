using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using UnityEngine.UI;

public class PlayerNameSelector : NetworkBehaviour
{
    private string playerName = "";
    private string message = "";

    GUIStyle labelStyle;
    GUIStyle buttonStyle;
    GUIStyle textFieldStyle;

    private static readonly int screenWidth = Screen.width;
    private static readonly int screenHeight = Screen.height;

    private static readonly int labelWidth = 200;
    private static readonly int labelHeight = 30;

    private static readonly int textFieldWidth = 200;
    private static readonly int textFieldHeight = 30;

    private static readonly int buttonWidth = 100;
    private static readonly int buttonHeight = 30;

    private static readonly int messageWidth = 300;
    private static readonly int messageHeight = 30;

    private void Awake()
    {
        labelStyle = new(GUI.skin.label)
        {
            fontSize = 20,
            alignment = TextAnchor.MiddleCenter
        };

        buttonStyle = new(GUI.skin.button)
        {
            fontSize = 20
        };

        textFieldStyle = new(GUI.skin.textField)
        {
            fontSize = 20
        };

        if (!IsOwner)
        {
            print("Destroyed parasite Awake");
            Destroy(this);
        }
    }

    private void OnGUI()
    {
        GUI.Label(new Rect((screenWidth - labelWidth) / 2, screenHeight / 2 - 60, labelWidth, labelHeight), "Enter your name:", labelStyle);

        playerName = GUI.TextField(new Rect((screenWidth - textFieldWidth) / 2, screenHeight / 2 - 20, textFieldWidth, textFieldHeight), playerName, textFieldStyle);

        if (GUI.Button(new Rect((screenWidth - buttonWidth) / 2, screenHeight / 2 + 20, buttonWidth, buttonHeight), "Login", buttonStyle))
        {
            if (!string.IsNullOrEmpty(playerName))
            {
                CheckWetherNameAvailableServerRpc();
            }
            else
            {
                message = "Please enter a name.";
            }
        }

        GUI.Label(
            new Rect((screenWidth - messageWidth) / 2, screenHeight / 2 + 60, messageWidth, messageHeight),
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
        print("tried been there");
        var teamSelectorComponent = gameObject.AddComponent<TeamSelector>();
        teamSelectorComponent.PlayerName = playerName;
        Destroy(this);
    }
}
