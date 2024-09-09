using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;

namespace NameSpace
{
    public sealed class OnScreenInfoDisplay : MonoBehaviour
    {
        public static OnScreenInfoDisplay Instance { get; private set; }


        private void Awake()
        {
            Instance = this;
        }
    }
}