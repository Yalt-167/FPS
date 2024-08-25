using GameManagement;

public interface IPlayerFrameMember
{
    public PlayerFrame PlayerFrame { get; set; }

    public void InitPlayerFrame(PlayerFrame playerFrame);
}
