using System.Collections;
using System.Collections.Generic;

using UnityEditor;
using UnityEngine;


namespace LobbyHandling
{
    [CustomEditor(typeof(LobbyHandler))]
    public sealed class LobbyHandlerCustomEditor : Editor
    {
        private LobbyHandler targetScript;
        private int spaceBetweenButtons;

        private void OnEnable()
        {
            targetScript = (LobbyHandler)target;
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            spaceBetweenButtons = targetScript.SpaceBetweenButtons;


            GUILayout.Space(spaceBetweenButtons * 2);
            if (GUILayout.Button("Sign In"))
            {
                targetScript.SignInAsync();
            }

            GUILayout.Space(spaceBetweenButtons * 2);
            GUILayout.Label("Those will use the options above");
            if (GUILayout.Button("Create"))
            {
                targetScript.CreateLobby(targetScript.LobbyName, targetScript.LobbyCapacity, targetScript.PrivateLobby, targetScript.Password);
            }

            GUILayout.Space(spaceBetweenButtons);
            if (GUILayout.Button("Edit lobby")) // add check to ensure you are the owner
            {
                if (string.IsNullOrEmpty(targetScript.TargetLobbyId))
                {
                    Debug.Log("You must provide the lobby id");
                }
                else
                {
                    targetScript.EditLobby(targetScript.TargetLobbyId, targetScript.LobbyName, targetScript.LobbyCapacity, targetScript.PrivateLobby, targetScript.Password);
                }
            }

            GUILayout.Space(spaceBetweenButtons);
            if (GUILayout.Button("Delete lobby")) // add check to ensure you are the owner
            {
                if (string.IsNullOrEmpty(targetScript.TargetLobbyId))
                {
                    Debug.Log("You must provide the lobby id");
                }
                else
                {
                    targetScript.DeleteLobby(targetScript.TargetLobbyId);
                }
                
            }

            GUILayout.Space(spaceBetweenButtons);
            if (GUILayout.Button("Join lobby by ID"))
            {
                if (string.IsNullOrEmpty(targetScript.TargetLobbyId))
                {
                    Debug.Log("You must provide the lobby id");
                }
                else
                {
                    targetScript.JoinLobbyByID(targetScript.TargetLobbyId, targetScript.Password);
                }

            }

            GUILayout.Space(spaceBetweenButtons);
            if (GUILayout.Button("Join lobby by code"))
            {
                if (string.IsNullOrEmpty(targetScript.TargetLobbyCode))
                {
                    Debug.Log("You must provide the lobby code");
                }
                else
                {
                    targetScript.JoinLobbyByCode(targetScript.TargetLobbyCode, targetScript.Password);
                }

            }

            GUILayout.Space(spaceBetweenButtons);
            if (GUILayout.Button("Join lobby with clipboard"))
            {
                targetScript.JoinLobbyByID(GUIUtility.systemCopyBuffer, targetScript.Password);
            }

            GUILayout.Space(spaceBetweenButtons);
            if (GUILayout.Button("Close lobby access"))
            {
                targetScript.CloseLobbyAcess();
            }

            GUILayout.Space(spaceBetweenButtons);
            if (GUILayout.Button("Quit current lobby"))
            {
                targetScript.QuitLobby();
            }

            GUILayout.Space(spaceBetweenButtons);
            GUILayout.Label("Utility");
            if (GUILayout.Button("List lobbies"))
            {
                targetScript.DisplayLobbies();
            }

            GUILayout.Space(spaceBetweenButtons);
            if (GUILayout.Button("Copy lobby ID"))
            {
                targetScript.CopyLobbyID();
            }

            GUILayout.Space(spaceBetweenButtons);
            if (GUILayout.Button("Copy lobby code"))
            {
                targetScript.CopyLobbyCode();
            }

            GUILayout.Space(spaceBetweenButtons);
            if (GUILayout.Button("Display current lobby data"))
            {
                targetScript.DisplayHostLobbyData();
            }

            GUILayout.Space(spaceBetweenButtons);
            if (GUILayout.Button("Display lobby data by id"))
            {
                targetScript.DisplayLobbyData(targetScript.TargetLobbyId);
            }

            GUILayout.Space(spaceBetweenButtons);
            if (GUILayout.Button("Display current lobby players"))
            {
                targetScript.DisplayHostLobbyPlayers();
            }

            GUILayout.Space(spaceBetweenButtons);
            if (GUILayout.Button("Display lobby players by ID"))
            {
                targetScript.DisplayPlayers(targetScript.TargetLobbyId);
            }

            GUILayout.Space(spaceBetweenButtons);
            GUILayout.Label("Relay");
            if (GUILayout.Button("List regions"))
            {
                targetScript.ListRegions();
            }

            GUILayout.Space(spaceBetweenButtons);
            GUILayout.Label("Solo Testing");
            if (GUILayout.Button("Start a solo session"))
            {
                targetScript.SelectLocalUnityTransport();
                GameManagement.GameNetworkManager.Singleton.StartHost();
            }

        }
    }
}