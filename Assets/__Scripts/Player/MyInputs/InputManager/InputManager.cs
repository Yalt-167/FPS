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
        public GeneralInputQuery GeneralInputs;
        public float cameraHorizontalSenitivity = 3f;
        public float cameraVerticalSensitivity = 3f;
        private bool doRenderMenu;
        private CurrentRebindMenu currentRebindMenu = CurrentRebindMenu.General;

        private void Awake()
        {
            MovementInputs.Init();
            CombatInputs.Init();
            GeneralInputs.Init();
        }

        private void Update()
        {
            doRenderMenu = GeneralInputs.TogglePauseMenu ? !doRenderMenu : doRenderMenu;

            if (doRenderMenu)
            {
                IInputQuery relevantInputQuery = currentRebindMenu switch
                {
                    CurrentRebindMenu.Movement => MovementInputs,
                    CurrentRebindMenu.Combat => CombatInputs,
                    CurrentRebindMenu.General => GeneralInputs,
                    _ => null
                };

                relevantInputQuery?.OnRenderRebindMenu();
            }
        }


    }
}