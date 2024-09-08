using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;

namespace Menus
{
    public static class MenuData
    {

        public static readonly int GameSettingsMenuWidth = 600;
        public static int GameSettingsMenuScrollableRegionHeight => Screen.height - 100;
        public static class RemapInput
        {
            public static readonly int ActionNameDisplayWidth = 200;
            public static readonly int KeybindDisplayWidth = 200;
            public static readonly int InputTypeDisplayWidth = 200;
        }
    }
}