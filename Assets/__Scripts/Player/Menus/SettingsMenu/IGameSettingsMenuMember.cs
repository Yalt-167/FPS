using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;

namespace Menus
{
    public interface IGameSettingsMenuMember
    {
        public GameSettingsMenu GameSettingsMenu { get; }

        public string MenuName { get; }

        public int MenuColumnsCount { get; }

        public void OnRenderMenu();
    }
}