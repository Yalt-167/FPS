namespace LobbyHandling
{
    public static class GameModes
    {
        public static readonly string ToBeVoted = nameof(ToBeVoted);
        public static readonly string TeamFight = nameof(TeamFight);
        public static readonly string DeathMatch = nameof(DeathMatch); // AKA FFA
        public static readonly string CaptureTheFlag = nameof(CaptureTheFlag);
        public static readonly string HardPoint = nameof(HardPoint);
        public static readonly string Escort = nameof(Escort);
        public static readonly string Arena = nameof(Arena);
        public static readonly string Breakthrough = nameof(Breakthrough);
    } 
}