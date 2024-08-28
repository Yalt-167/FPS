using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;

namespace Inputs
{
    public sealed class GenericInputQuery : InputQuery
    {
        public FixedKeybind TogglePauseMenu = new(KeyCode.Escape, PlayerActionActivationType.OnKeyDown);

        public override void Init()
        {
            TogglePauseMenu.Init();
        }
    }
}