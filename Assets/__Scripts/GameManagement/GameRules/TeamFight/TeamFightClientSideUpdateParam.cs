using Unity.Netcode;

namespace GameManagement
{
    [System.Serializable]
    public struct TeamFightClientSideUpdateParam : IClientSideGameRuleUpdateParam
    {
        public ClientSideGameRuleUpdateParamType GameRuleType { get; private set; }

        /// <summary>
        /// Placeholder param in order to avoid a parameterless constructor (unavailable in my C# version)
        /// </summary>
        /// <param name="_"></param>
        public TeamFightClientSideUpdateParam(object _)
        {
            GameRuleType = ClientSideGameRuleUpdateParamType.TeamFight;
        }

        public void Deserialize(ref FastBufferReader reader)
        {
            if (reader.Length < sizeof(ClientSideGameRuleUpdateParamType))
            {
                UnityEngine.Debug.LogError("Buffer does not contain enough data for deserialization.");
                return; // Handle accordingly
            }

            reader.ReadValueSafe(out ClientSideGameRuleUpdateParamType gameRuleType);

            GameRuleType = gameRuleType;
        }

        public void Serialize(ref FastBufferWriter writer)
        {
            writer.WriteValueSafe(GameRuleType);
        }
    }
}