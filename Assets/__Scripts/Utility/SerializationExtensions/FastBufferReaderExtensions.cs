public static class FastBufferReaderExtensions
{
    public static void ReadValueSafe(this Unity.Netcode.FastBufferReader reader, out GameManagement.IClientSideGameRuleUpdateParam param)
    {
        reader.ReadValueSafe(out GameManagement.ClientSideGameRuleUpdateParamType clientSideGameRuleUpdateParamType);

        GameManagement.IClientSideGameRuleUpdateParam instance = clientSideGameRuleUpdateParamType switch
        {
            GameManagement.ClientSideGameRuleUpdateParamType.None => throw new System.Exception("Compiler Happy"),
            GameManagement.ClientSideGameRuleUpdateParamType.TeamFight => (GameManagement.IClientSideGameRuleUpdateParam)new GameManagement.TeamFightClientSideUpdateParam(),
            _ => throw new System.Exception("This ClientSideUpdateParamType does not exist"),
        };

        instance.Deserialize(ref reader);

        param = instance;
    }
}