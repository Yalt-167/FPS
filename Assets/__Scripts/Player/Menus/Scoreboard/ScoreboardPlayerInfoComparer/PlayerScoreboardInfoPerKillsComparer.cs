using System;
using System.Collections;
using System.Collections.Generic;

namespace Menus
{
    public sealed class PlayerScoreboardInfoPerKillsComparer : IComparer<PlayerScoreboardInfos>
    {
        public int Compare(PlayerScoreboardInfos x, PlayerScoreboardInfos y)
        {
            return y.Kills.CompareTo(x.Kills);
        }
    }
}