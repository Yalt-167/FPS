using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;

namespace Inputs
{
    [Serializable]
    public sealed class MovementInputQuery : IInputQuery
    {
        public FixedKeybind InitiateForward = new(KeyCode.Z, PlayerActionActivationType.OnKeyDown, nameof(InitiateForward));
        public FixedKeybind Forward = new(KeyCode.Z, PlayerActionActivationType.OnKeyHeld, nameof(Forward));

        public FixedKeybind InitiateBack = new(KeyCode.S, PlayerActionActivationType.OnKeyDown, nameof(InitiateBack));
        public FixedKeybind Back = new(KeyCode.S, PlayerActionActivationType.OnKeyHeld, nameof(Back));

        public FixedKeybind InitiateRight = new(KeyCode.D, PlayerActionActivationType.OnKeyDown, nameof(InitiateRight));
        public FixedKeybind HoldRightForTime = new(KeyCode.D, PlayerActionActivationType.OnHeldForTime, nameof(HoldRightForTime));
        public FixedKeybind Right = new(KeyCode.D, PlayerActionActivationType.OnKeyHeld, nameof(Right));

        public FixedKeybind InitiateLeft = new(KeyCode.Q, PlayerActionActivationType.OnKeyDown, nameof(InitiateLeft));
        public FixedKeybind HoldLeftForTime = new(KeyCode.Q, PlayerActionActivationType.OnHeldForTime, nameof(HoldLeftForTime));
        public FixedKeybind Left = new(KeyCode.Q, PlayerActionActivationType.OnKeyHeld, nameof(Left));

        public FixedKeybind InitiateJump = new(KeyCode.Space, PlayerActionActivationType.OnKeyDown, nameof(InitiateJump));
        public FixedKeybind HoldJump = new(KeyCode.Space, PlayerActionActivationType.OnKeyHeld, nameof(HoldJump));
        public FixedKeybind InterruptJump = new(KeyCode.Space, PlayerActionActivationType.OnKeyUp, nameof(InterruptJump));

        public FixedKeybind InitiateSlide = new(KeyCode.LeftShift, PlayerActionActivationType.OnKeyDown, nameof(InitiateSlide));
        public FixedKeybind HoldSlide = new(KeyCode.LeftShift, PlayerActionActivationType.OnKeyHeld, nameof(HoldSlide));

        public VariableKeybind HoldCrouch = new(KeyCode.LeftControl, new List<PlayerActionActivationType>() { PlayerActionActivationType.OnKeyHeld, PlayerActionActivationType.Toggle }, nameof(HoldCrouch));

        public FixedKeybind Dash = new(KeyCode.Alpha9, PlayerActionActivationType.OnKeyDown, nameof(Dash));

        public FixedKeybind InitiateGrapplingHook = new(KeyCode.X, PlayerActionActivationType.OnKeyDown, nameof(InitiateGrapplingHook));
        public FixedKeybind HoldGrapplingHook = new(KeyCode.X, PlayerActionActivationType.OnKeyHeld, nameof(HoldGrapplingHook));
        public FixedKeybind ReleaseGrapplingHook = new(KeyCode.X, PlayerActionActivationType.OnKeyUp, nameof(ReleaseGrapplingHook));

        public FixedKeybind SwitchCameraPosition = new(KeyCode.W, PlayerActionActivationType.OnKeyDown, nameof(SwitchCameraPosition));
        public VariableKeybind QuickReset = new(KeyCode.X, new List<PlayerActionActivationType>() { PlayerActionActivationType.OnKeyDown, PlayerActionActivationType.OnHeldForTime }, .5f, nameof(QuickReset));


        public List<Keybind> Keybinds { get; private set; }

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
                keybind.OnRenderRebindMenu();
            }
        }

        public void RegisterKeybind(Keybind bind)
        {
            Keybinds.Add(bind);
        }
    }
}