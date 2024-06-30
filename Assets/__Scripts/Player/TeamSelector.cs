using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class TeamSelector : MonoBehaviour
{
    private static readonly string Team1 = "Team 1";
    private static readonly string Team2 = "Team 2";

    public string PlayerName;

    private void OnGUI()
    {
        GUILayout.BeginArea(new Rect(10, 10, 300, 300));

//#error Would raise that I assume


        if (GUILayout.Button(Team1)) OnTeamSelected(1);
        if (GUILayout.Button(Team2)) OnTeamSelected(2);
        
        GUILayout.EndArea();
    }
    

    private void OnTeamSelected(ushort teamID)
    {       
        print("0");
        GetComponent<PlayerHealthNetworked>().RequestSetTeamServerRpc(teamID);
        print("1");


        Destroy(this);
    }

    
}
