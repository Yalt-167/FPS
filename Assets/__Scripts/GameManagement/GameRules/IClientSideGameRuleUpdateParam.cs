using Unity.Netcode;

namespace GameManagement
{
    public interface IClientSideGameRuleUpdateParam : INetworkSerializable
    {
        //public void Serialize(ref FastBufferWriter writer);
        //public void Deserialize(ref FastBufferReader reader);
    }
}