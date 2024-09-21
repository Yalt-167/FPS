using GameManagement;
using System;
using Unity.Netcode;


namespace GameManagement
{
    public interface IClientSideGameRuleUpdateParam 
    {
        public ClientSideGameRuleUpdateParamType GameRuleType { get; }
        public void Serialize(ref FastBufferWriter writer);

        public void Deserialize(ref FastBufferReader reader);

    }
}