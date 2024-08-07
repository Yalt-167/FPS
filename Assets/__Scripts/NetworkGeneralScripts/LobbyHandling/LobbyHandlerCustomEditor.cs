using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace LobbyHandling
{
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
            GUILayout.Label("Create a lobby using the options above");
            if (GUILayout.Button("Create"))
            {
                targetScript.CreateLobby(targetScript.LobbyName, targetScript.LobbyCapacity, targetScript.PrivateLobby, targetScript.Password);
            }

            GUILayout.Space(spaceBetweenButtons);

            if (GUILayout.Button("Edit lobby"))
            {
                targetScript.EditLobby(targetScript.TargetLobbyId, targetScript.LobbyName, targetScript.LobbyCapacity, targetScript.PrivateLobby, targetScript.Password);
            }

            GUILayout.Space(spaceBetweenButtons);
            if (GUILayout.Button("Delete lobby"))
            {
                targetScript.DeleteLobby();
            }

            GUILayout.Space(spaceBetweenButtons);
            if (GUILayout.Button("Join lobby by id"))
            {
                targetScript.JoinLobbyByID();
            }

            GUILayout.Space(spaceBetweenButtons);
            if (GUILayout.Button("Join lobby by code"))
            {
                targetScript.JoinLobbyByCode();
            }

            GUILayout.Space(spaceBetweenButtons);
            if (GUILayout.Button("List lobbies"))
            {
                targetScript.ListLobbies();
            }

            GUILayout.Space(spaceBetweenButtons);
            if (GUILayout.Button("Copy ID"))
            {
                targetScript.CopyLobbyID();
            }

            GUILayout.Space(spaceBetweenButtons);
            if (GUILayout.Button("CopyCode"))
            {
                targetScript.CopyLobbyCode();
            }
        }
    }
}