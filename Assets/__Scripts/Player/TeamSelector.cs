using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class TeamSelector : MonoBehaviour
{
    private static readonly string Team0 = "Team 0";
    private static readonly string Team1 = "Team 1";

    public string PlayerName;

    private void OnGUI()
    {
        GUILayout.BeginArea(new Rect(10, 10, 300, 300));
        
        if (GUILayout.Button(Team0)) OnTeamSelected(0);
        if (GUILayout.Button(Team1)) OnTeamSelected(1);
        
        GUILayout.EndArea();
    }

    private void OnTeamSelected(ushort teamID)
    {
        Cursor.lockState = CursorLockMode.Locked; //
        Cursor.visible = false; // those two may cause issues when destroying the script on remote players // chekc when loggin concurrently

        print("0");
        Game.Manager.RegisterPlayerServerRpc(
            new(
                PlayerName,
                teamID,
                GetComponent<NetworkObject>().NetworkObjectId
                )
        );
        print("1");
        GetComponent<PlayerHealthNetworked>().RequestSetTeamServerRpc(teamID);

        print("2");
        GetComponent<HandlePlayerNetworkBehaviour>().ToggleControls(true);

        Destroy(this);
    }

    
}
