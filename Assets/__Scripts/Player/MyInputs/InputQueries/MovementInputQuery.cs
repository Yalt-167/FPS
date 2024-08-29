using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;

namespace Inputs
{
    [Serializable]
    public sealed class MovementInputQuery : IInputQuery
    {
        public FixedKeybind InitiateForward = new(KeyCode.Z, PlayerActionActivationType.OnKeyDown);
        public FixedKeybind Forward = new(KeyCode.Z, PlayerActionActivationType.OnKeyHeld);

        public FixedKeybind InitiateBack = new(KeyCode.S, PlayerActionActivationType.OnKeyDown);
        public FixedKeybind Back = new(KeyCode.S, PlayerActionActivationType.OnKeyHeld);

        public FixedKeybind InitiateRight = new(KeyCode.D, PlayerActionActivationType.OnKeyDown);
        public FixedKeybind HoldRightForTime = new(KeyCode.D, PlayerActionActivationType.OnHeldForTime);
        public FixedKeybind Right = new(KeyCode.D, PlayerActionActivationType.OnKeyHeld);

        public FixedKeybind InitiateLeft = new(KeyCode.Q, PlayerActionActivationType.OnKeyDown);
        public FixedKeybind HoldLeftForTime = new(KeyCode.Q, PlayerActionActivationType.OnHeldForTime);
        public FixedKeybind Left = new(KeyCode.Q, PlayerActionActivationType.OnKeyHeld);

        public FixedKeybind InitiateJump = new(KeyCode.Space, PlayerActionActivationType.OnKeyDown);
        public FixedKeybind HoldJump = new(KeyCode.Space, PlayerActionActivationType.OnKeyHeld);
        public FixedKeybind InterruptJump = new(KeyCode.Space, PlayerActionActivationType.OnKeyUp);

        public FixedKeybind InitiateSlide = new(KeyCode.LeftShift, PlayerActionActivationType.OnKeyDown);
        public FixedKeybind HoldSlide = new(KeyCode.LeftShift, PlayerActionActivationType.OnKeyHeld);

        public VariableKeybind HoldCrouch = new(KeyCode.LeftControl, new List<PlayerActionActivationType>() { PlayerActionActivationType.OnKeyHeld, PlayerActionActivationType.Toggle });

        public FixedKeybind Dash = new(KeyCode.Alpha9, PlayerActionActivationType.OnKeyDown);

        public FixedKeybind InitiateGrapplingHook = new(KeyCode.X, PlayerActionActivationType.OnKeyDown);
        public FixedKeybind HoldGrapplingHook = new(KeyCode.X, PlayerActionActivationType.OnKeyHeld);
        public FixedKeybind ReleaseGrapplingHook = new(KeyCode.X, PlayerActionActivationType.OnKeyUp);

        public FixedKeybind SwitchCameraPosition = new(KeyCode.W, PlayerActionActivationType.OnKeyDown);
        public VariableKeybind QuickReset = new(KeyCode.X, new List<PlayerActionActivationType>() { PlayerActionActivationType.OnKeyDown, PlayerActionActivationType.OnHeldForTime }, .5f);

        public List<Keybind> Keybinds { get; private set; } = new();

        public bool DoRenderRebindMenu { get; private set; }

        public void Init()
        {
            InitiateForward.Init(this);
            Forward.Init(this);

            InitiateBack.Init(this);
            Back.Init(this);

            InitiateRight.Init(this);
            Right.Init(this);
            HoldRightForTime.Init(this);

            InitiateLeft.Init(this);
            Left.Init(this);
            HoldLeftForTime.Init(this);

            InitiateJump.Init(this);
            HoldJump.Init(this);
            InterruptJump.Init(this);

            InitiateSlide.Init(this);
            HoldSlide.Init(this);
            HoldCrouch.Init(this);
            Dash.Init(this);

            InitiateGrapplingHook.Init(this);
            HoldGrapplingHook.Init(this);
            ReleaseGrapplingHook.Init(this);

            SwitchCameraPosition.Init(this);

            QuickReset.Init(this);
        }

        public void OnRenderRebindMenu()
        {
            foreach (var keybind in Keybinds)
            {
                keybind.OnRenderRebingMenu();
            }
        }

        public void RegisterKeybind(Keybind bind)
        {
            Keybinds.Add(bind);
        }
    }
}