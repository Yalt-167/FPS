using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class PlayerDataForLobby
{
    public static readonly string Username = nameof(Username);
    public static readonly string Kills = nameof(Kills);
    public static readonly string Deaths = nameof(Deaths);
    public static readonly string Assists = nameof(Assists);
    public static readonly string Damage = nameof(Damage);
    public static readonly string Accuracy = nameof(Accuracy); // private
}

// polling rate not linear ? when some guy presses tab -> query else no need?