using System.Collections;
using System.Collections.Generic;

using UnityEngine;

using GameManagement;

public interface IPlayerFrameMember
{
    public PlayerFrame PlayerFrame { get; set; }

    public void InitPlayerFrame(PlayerFrame playerFrame);
}
