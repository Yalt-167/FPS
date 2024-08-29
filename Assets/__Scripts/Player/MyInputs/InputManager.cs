using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;

namespace Inputs
{
    public sealed class InputManager : MonoBehaviour
    {
        public MovementInputQuery MovementInputs;
        public CombatInputQuery CombatInputs;
        public CombatInputQuery GenericInputs;
        public float cameraXSensitivity = 4f;
        public float cameraYSensitivity = 4f;


        private void Awake()
        {
            MovementInputs.Init();
            CombatInputs.Init();
            GenericInputs.Init();
        }
    }
}