using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(LobbyHandler))]
public class LobbyHandlerCustomEditor : Editor
{

    private LobbyHandler targetScript;
    private int spaceBetweenButtons;

    public void Awake()
    {
        targetScript = (LobbyHandler)target;
        spaceBetweenButtons = targetScript.SpaceBetweenButtons;
    }

    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        targetScript = (LobbyHandler)target;
        spaceBetweenButtons = targetScript.SpaceBetweenButtons;
        GUILayout.Space(spaceBetweenButtons);
        targetScript.CreateLobbyParamString = GUILayout.TextField(targetScript.CreateLobbyParamString);
        if (GUILayout.Button("Create lobby"))
        {
            targetScript.CreateLobby();
        }

        targetScript.EditLobbyParamString = GUILayout.TextField(targetScript.EditLobbyParamString);
        GUILayout.Space(spaceBetweenButtons);
        if (GUILayout.Button("Edit lobby"))
        {
            targetScript.EditLobby();
        }

        targetScript.DeleteLobbyParamString = GUILayout.TextField(targetScript.DeleteLobbyParamString);
        GUILayout.Space(spaceBetweenButtons);
        if (GUILayout.Button("Delete lobby"))
        {
            targetScript.DeleteLobby();
        }

        targetScript.JoinLobbyByIDParamString = GUILayout.TextField(targetScript.JoinLobbyByIDParamString);
        GUILayout.Space(spaceBetweenButtons);
        if (GUILayout.Button("Join lobby by id"))
        {
            targetScript.JoinLobbyByID();
        }

        targetScript.JoinLobbyByCodeParamString = GUILayout.TextField(targetScript.JoinLobbyByCodeParamString);
        GUILayout.Space(spaceBetweenButtons);
        if (GUILayout.Button("Join lobby by code"))
        {
            targetScript.JoinLobbyByCode();
        }

        targetScript.ListLobbiesParamString = GUILayout.TextField(targetScript.ListLobbiesParamString);
        GUILayout.Space(spaceBetweenButtons);
        if (GUILayout.Button("List lobbies"))
        {
            targetScript.ListLobbies();
        }


    }
}
