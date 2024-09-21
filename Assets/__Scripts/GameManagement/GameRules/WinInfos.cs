namespace GameManagement
{
    [System.Serializable]
    public struct WinInfos : Unity.Netcode.INetworkSerializable
    {
        public int WinningTeamNumber; // 0 if none yet

        public WinInfos(int teamNumber)
        {
            WinningTeamNumber = teamNumber;
        }

        public readonly bool HasWinner()
        {
            return WinningTeamNumber != 0;
        }

        public void NetworkSerialize<T>(Unity.Netcode.BufferSerializer<T> serializer) where T : Unity.Netcode.IReaderWriter
        {
            serializer.SerializeValue(ref WinningTeamNumber);
        }
    }
}