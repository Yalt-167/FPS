namespace GameManagement
{
    [System.Serializable]
    public struct WinInfos
    {
        public int WinningTeamNumber; // 0 if none yet

        public WinInfos(int teamNumber)
        {
            WinningTeamNumber = teamNumber;
        }
    }
}