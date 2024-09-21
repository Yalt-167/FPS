using Unity.Netcode;

namespace GameManagement
{
    [System.Serializable]
    public struct WinInfos : INetworkSerializable
    {
        public int WinningTeamNumber; // 0 if none yet

        public WinInfos(int teamNumber)
        {
            WinningTeamNumber = teamNumber;
        }

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref WinningTeamNumber);
        }
    }
}