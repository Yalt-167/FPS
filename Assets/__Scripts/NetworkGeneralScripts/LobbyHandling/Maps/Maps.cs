using System;
using static LobbyHandling.Maps;

namespace LobbyHandling
{
	public static class Maps // redo that with per mode maps
	{
        public static Type GetRelevantTypeForGamemode(string gamemode)
        {
            return gamemode switch
            {
                nameof(TeamFight) => typeof(TeamFight),
                nameof(DeathMatch) => typeof(DeathMatch),
                nameof(CaptureTheFlag) => typeof(CaptureTheFlag),
                nameof(HardPoint) => typeof(HardPoint),
                nameof(Escort) => typeof(Escort),
                nameof(Arena) => typeof(Arena),
                nameof(Breakthrough) => typeof(Breakthrough),
                nameof(ToBeVoted) => typeof(Maps),
                _ => typeof(Maps),
            };
        }

		public static readonly string ToBeVoted = nameof(ToBeVoted);
		public static class TeamFight
		{
            public static readonly string Map1 = nameof(Map1);
            public static readonly string Map2 = nameof(Map2);
            public static readonly string Map3 = nameof(Map3);
        }

        public static class DeathMatch
        {
            public static readonly string Map1 = nameof(Map1);
            public static readonly string Map2 = nameof(Map2);
            public static readonly string Map3 = nameof(Map3);
        }

        public static class Escort
        {
            public static readonly string Map1 = nameof(Map1);
            public static readonly string Map2 = nameof(Map2);
            public static readonly string Map3 = nameof(Map3);
        }

        public static class CaptureTheFlag
        {
            public static readonly string Map1 = nameof(Map1);
            public static readonly string Map2 = nameof(Map2);
            public static readonly string Map3 = nameof(Map3);
        }

        public static class HardPoint
        {
            public static readonly string Map1 = nameof(Map1);
            public static readonly string Map2 = nameof(Map2);
            public static readonly string Map3 = nameof(Map3);
        }

        public static class Arena
        {
            public static readonly string Map1 = nameof(Map1);
        }

        public static class Breakthrough
        {
            public static readonly string Map1 = nameof(Map1);
            public static readonly string Map2 = nameof(Map2);
            public static readonly string Map3 = nameof(Map3);
        }
    } 
}
