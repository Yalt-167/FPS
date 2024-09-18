using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;

namespace GameManagement
{
    public interface IGameRuleState
    {
        public WinInfos WinInfos { get; }
    }
}