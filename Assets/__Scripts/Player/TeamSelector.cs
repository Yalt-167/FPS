using System.Collections;
using System.Collections.Generic;
using Unity.Multiplayer.Samples.Utilities.ClientAuthority;
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
        
        if (GUILayout.Button(Team0))
        {
            OnTeamSelected(0);
        }
        if (GUILayout.Button(Team1))
        {
            OnTeamSelected(1);
        }
        
        GUILayout.EndArea();
    }

    private void OnTeamSelected(ushort teamID)
    {
        PlayerHealthNetworked healthComponent;
        Cursor.lockState = CursorLockMode.Locked; //
        Cursor.visible = false; // those two may cause issues when destroying the script on remote players // chekc when loggin concurrently

        healthComponent = GetComponent<PlayerHealthNetworked>();

        Game.Manager.RegisterPlayerServerRpc(
            new(
                PlayerName,
                teamID,
                GetComponent<NetworkObject>().NetworkObjectId
                )
        );
        healthComponent.RequestSetTeamServerRpc(teamID);
        Destroy(this);
    }

    
}
