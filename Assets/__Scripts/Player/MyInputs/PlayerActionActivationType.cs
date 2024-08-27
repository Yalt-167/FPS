using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;

namespace Inputs
{
    public enum PlayerActionActivationType
    {
        OnKeyDown,
        OnKeyUp,
        OnKeyHeld,
        Toggle,
        OnHeldForTime,
        //StartedHoldingAfterEvent
    }
}