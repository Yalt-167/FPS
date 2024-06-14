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

    // Define the style for the button
    GUIStyle buttonStyle;

    // Define the style for the text field
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
    }
    private void OnGUI()
    {
        // Define the style for the label

        // Display the label
        GUI.Label(new Rect((screenWidth - labelWidth) / 2, screenHeight / 2 - 60, labelWidth, labelHeight), "Enter your name:", labelStyle);

        // Display the text field
        playerName = GUI.TextField(new Rect((screenWidth - textFieldWidth) / 2, screenHeight / 2 - 20, textFieldWidth, textFieldHeight), playerName, textFieldStyle);

        // Display the button
        if (GUI.Button(new Rect((screenWidth - buttonWidth) / 2, screenHeight / 2 + 20, buttonWidth, buttonHeight), "Login", buttonStyle))
        {
            // Handle the button click
            if (!string.IsNullOrEmpty(playerName))
            {
                CheckWetherNameAvailableServerRpc();
                message = $"Welcome, {playerName} !";
            }
            else
            {
                message = "Please enter a name.";
            }
        }

        // Display the message
        GUI.Label(new Rect((screenWidth - messageWidth) / 2, screenHeight / 2 + 60, messageWidth, messageHeight), message, labelStyle);
    }

    [Rpc(SendTo.Server)]
    private void CheckWetherNameAvailableServerRpc()
    {
        if (Game.Manager.PlayerWithNameExist(playerName))
        {

        }
        else
        {
            ContinueOntoTeamSelector();
        }
    }

    private void ContinueOntoTeamSelector()
    {
        var teamSelectorComponent = gameObject.AddComponent<TeamSelector>();
        teamSelectorComponent.PlayerName = playerName;
        Destroy(this);
    }
}
