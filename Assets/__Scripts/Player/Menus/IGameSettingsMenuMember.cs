using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;

namespace Menus
{
    public interface IGameSettingsMenuMember
    {
        public string MenuName { get; }

        public int MenuTabsCount { get; }

        public void OnRenderMenu();

    }
}