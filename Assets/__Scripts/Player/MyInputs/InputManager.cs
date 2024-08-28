using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;

namespace Inputs
{
    public sealed class InputManager : MonoBehaviour
    {
        public MovementInputQuery movementInputs;
        public CombatInputQuery combatInputs;
        public CombatInputQuery genericInputs;


        private void Awake()
        {
            movementInputs.Init();
            combatInputs.Init();
            genericInputs.Init();
        }
    }
}