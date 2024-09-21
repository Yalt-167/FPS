namespace GameManagement
{
    public interface IMustBeInitiatedAfterPlayerFrame
    {
        public void InitOnLocalPlayer();
        public void InitOnRemotePlayer();
        public void InitOnAnyPlayer();
    }
}