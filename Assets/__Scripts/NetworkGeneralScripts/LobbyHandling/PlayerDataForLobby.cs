using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class PlayerDataForLobby
{
    public static readonly string Username = "Username";
    public static readonly string Kills = "Kills";
    public static readonly string Deaths = "Deaths";
    public static readonly string Assists = "Assists";
    public static readonly string Damage = "Damage";
    public static readonly string Accuracy = "Accuracy"; // private
}

// polling rate not linear ? when some guy presses tab -> query else no need?