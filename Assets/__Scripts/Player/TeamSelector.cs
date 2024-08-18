using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;

public sealed class TeamSelector : MonoBehaviour
{
    private static readonly string Team1 = "Team 1";
    private static readonly string Team2 = "Team 2";

    private static readonly Rect areaRect = new(10, 10, 300, 300);

    private void OnGUI()
    {
        GUILayout.BeginArea(areaRect);


        if (GUILayout.Button(Team1)) OnTeamSelected(1);
        if (GUILayout.Button(Team2)) OnTeamSelected(2);
        
        GUILayout.EndArea();
    }
    
    private void OnTeamSelected(ushort teamID)
    {       
        GetComponent<GameManagement.PlayerFrame>().RequestSetTeamServerRpc(teamID);

        Destroy(this);
    }
}
