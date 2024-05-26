using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class TeamSelector : MonoBehaviour
{
    private static readonly string Team0 = "Team 0";
    private static readonly string Team1 = "Team 1";

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
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false; // those two may cause issues when destroying the script on remote players
        GetComponent<PlayerHealthNetworked>().RequestSetTeamServerRpc(teamID);
        Destroy(this);
    }
}
