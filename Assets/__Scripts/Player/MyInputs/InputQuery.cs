using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;

namespace Inputs
{
    public abstract class InputQuery : MonoBehaviour
    {
        public static InputQuery Manager;
        private void Awake() { Manager = this; }

        public void InitBinds()
        {

        }

        public abstract void Init();
    }
}