using System;
using System.Collections;
using System.Collections.Generic;

namespace Menus
{
    public sealed class PlayerScoreboardInfoPerRatioComparer : IComparer<PlayerScoreboardInfos>
    {
        public int Compare(PlayerScoreboardInfos x, PlayerScoreboardInfos y)
        {
            return (y.Kills - y.Deaths).CompareTo(x.Kills - x.Deaths);
        }
    }
}