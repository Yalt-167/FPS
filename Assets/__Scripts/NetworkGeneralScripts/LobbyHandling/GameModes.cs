using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LobbyHandling
{
    public static class GameModes
    {
        public static readonly string ToBeVoted = nameof(ToBeVoted);
        public static readonly string TeamMatch = nameof(TeamMatch);
        public static readonly string DeathMatch = nameof(DeathMatch);
    } 
}
