namespace Menus
{
    public struct PlayerScoreboardInfos
    {
        public int Team;
        public string Name;
        public string CurrentChampion;
        public int Kills;
        public int Deaths;
        public int Assists;
        public PlayerScoreboardInfos(int team, string playerName)
        {
            Team = team;
            Name = playerName;
            CurrentChampion = "Champion";
            Kills = 0;
            Deaths = 0;
            Assists = 0;
        }

        public readonly override string ToString()
        {
            return Name; 
        }
    }
}