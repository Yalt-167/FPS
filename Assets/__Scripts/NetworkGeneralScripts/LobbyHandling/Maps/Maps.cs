namespace LobbyHandling
{
	public static class Maps
	{
        public static Type GetRelevantTypeForMapOfGamemode(string gamemode)
        {
            return gamemode switch
            {
                nameof(GameModes.TeamFight) => typeof(TeamFight),
                nameof(GameModes.DeathMatch) => typeof(DeathMatch),
                nameof(GameModes.CaptureTheFlag) => typeof(CaptureTheFlag),
                nameof(GameModes.HardPoint) => typeof(HardPoint),
                nameof(GameModes.Escort) => typeof(Escort),
                nameof(GameModes.Arena) => typeof(Arena),
                nameof(GameModes.Breakthrough) => typeof(Breakthrough),
                nameof(GameModes.ToBeVoted) => typeof(Maps),
                _ => throw new System.Exception($"This name {gamemode} is not recognized"),
            };
        }

		public static readonly string ToBeVoted = nameof(ToBeVoted);
        public static class TeamFight
        {
            public static readonly string TeamFightMap1 = nameof(TeamFightMap1);
            public static readonly string TeamFightMap2 = nameof(TeamFightMap2);
            public static readonly string TeamFightMap3 = nameof(TeamFightMap3);
        }

        public static class DeathMatch
        {
            public static readonly string DeathMatchMap1 = nameof(DeathMatchMap1);
            public static readonly string DeathMatchMap2 = nameof(DeathMatchMap2);
            public static readonly string DeathMatchMap3 = nameof(DeathMatchMap3);
        }

        public static class Escort
        {
            public static readonly string EscortMap1 = nameof(EscortMap1);
            public static readonly string EscortMap2 = nameof(EscortMap2);
            public static readonly string EscortMap3 = nameof(EscortMap3);
        }

        public static class CaptureTheFlag
        {
            public static readonly string CaptureTheFlagMap1 = nameof(CaptureTheFlagMap1);
            public static readonly string CaptureTheFlagMap2 = nameof(CaptureTheFlagMap2);
            public static readonly string CaptureTheFlagMap3 = nameof(CaptureTheFlagMap3);
        }

        public static class HardPoint
        {
            public static readonly string HardPointMap1 = nameof(HardPointMap1);
            public static readonly string HardPointMap2 = nameof(HardPointMap2);
            public static readonly string HardPointMap3 = nameof(HardPointMap3);
        }

        public static class Arena
        {
            public static readonly string ArenaMap1 = nameof(ArenaMap1);
        }

        public static class Breakthrough
        {
            public static readonly string BreakthroughMap1 = nameof(BreakthroughMap1);
            public static readonly string BreakthroughMap2 = nameof(BreakthroughMap2);
            public static readonly string BreakthroughMap3 = nameof(BreakthroughMap3);
        }

    }
}
