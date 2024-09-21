public static class FastBufferWriterExtensions
{
    public static void WriteValueSafe(this Unity.Netcode.FastBufferWriter writer, in GameManagement.IClientSideGameRuleUpdateParam param)
    {
        param.Serialize(ref writer);
    }
}