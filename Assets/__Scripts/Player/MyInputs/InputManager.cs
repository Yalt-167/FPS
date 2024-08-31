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
        public GenericInputQuery GenericInputs;
        public float cameraHorizontalSenitivity = 3f;
        public float cameraVerticalSensitivity = 3f;
        private bool doRenderMenu;

        private void Awake()
        {
            MovementInputs.Init();
            CombatInputs.Init();
            GenericInputs.Init();
        }

        private void Update()
        {
            doRenderMenu = GenericInputs.TogglePauseMenu ? !doRenderMenu : doRenderMenu;

            if (doRenderMenu)
            {

            }
        }


    }
}