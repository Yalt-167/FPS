using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;

namespace Inputs
{
    [Serializable]
    public sealed class MovementInputQuery : IInputQuery
    {
        //public FixedKeybind InitiateForward = new(KeyCode.Z, PlayerInputType.OnKeyDown, nameof(InitiateForward));
        //public FixedKeybind Forward = new(KeyCode.Z, PlayerInputType.OnKeyHeld, nameof(Forward));
        public GroupKeybind Forward = new(KeyCode.Z, new InputType[] { InputType.OnKeyDown, InputType.OnKeyHeld }, nameof(Forward));
        public GroupKeybind Back = new(KeyCode.S, new InputType[] { InputType.OnKeyDown, InputType.OnKeyHeld }, nameof(Back));
        public GroupKeybind Right = new(KeyCode.D, new InputType[] { InputType.OnKeyDown, InputType.OnKeyHeld, InputType.OnKeyHeldForTime }, nameof(Right));
        public GroupKeybind Left = new(KeyCode.Q, new InputType[] { InputType.OnKeyDown, InputType.OnKeyHeld, InputType.OnKeyHeldForTime }, nameof(Left));
        public GroupKeybind Jump = new(KeyCode.Space, new InputType[] { InputType.OnKeyDown, InputType.OnKeyHeld, InputType.OnKeyUp }, nameof(Jump));
        public GroupKeybind Slide = new(KeyCode.LeftShift, new InputType[] { InputType.OnKeyDown, InputType.OnKeyHeld }, nameof(Slide));
        public GroupKeybind GrapplingHook = new(KeyCode.Space, new InputType[] { InputType.OnKeyDown, InputType.OnKeyHeld, InputType.OnKeyUp }, nameof(GrapplingHook));

        //public FixedKeybind InitiateRight = new(KeyCode.D, PlayerInputType.OnKeyDown, nameof(InitiateRight));
        //public FixedKeybind HoldRightForTime = new(KeyCode.D, PlayerInputType.OnHeldForTime, nameof(HoldRightForTime));
        //public FixedKeybind Right = new(KeyCode.D, PlayerInputType.OnKeyHeld, nameof(Right));

        //public FixedKeybind InitiateLeft = new(KeyCode.Q, PlayerInputType.OnKeyDown, nameof(InitiateLeft));
        //public FixedKeybind HoldLeftForTime = new(KeyCode.Q, PlayerInputType.OnHeldForTime, nameof(HoldLeftForTime));
        //public FixedKeybind Left = new(KeyCode.Q, PlayerInputType.OnKeyHeld, nameof(Left));

        //public FixedKeybind InitiateJump = new(KeyCode.Space, PlayerInputType.OnKeyDown, nameof(InitiateJump));
        //public FixedKeybind HoldJump = new(KeyCode.Space, PlayerInputType.OnKeyHeld, nameof(HoldJump));
        //public FixedKeybind InterruptJump = new(KeyCode.Space, PlayerInputType.OnKeyUp, nameof(InterruptJump));

        //public FixedKeybind InitiateSlide = new(KeyCode.LeftShift, PlayerInputType.OnKeyDown, nameof(InitiateSlide));
        //public FixedKeybind HoldSlide = new(KeyCode.LeftShift, PlayerInputType.OnKeyHeld, nameof(HoldSlide));

        public VariableKeybind HoldCrouch = new(KeyCode.LeftControl, new List<InputType>() { InputType.OnKeyHeld, InputType.Toggle }, nameof(HoldCrouch));

        public FixedKeybind Dash = new(KeyCode.Alpha9, InputType.OnKeyDown, nameof(Dash));

        //public FixedKeybind InitiateGrapplingHook = new(KeyCode.X, PlayerInputType.OnKeyDown, nameof(InitiateGrapplingHook));
        //public FixedKeybind HoldGrapplingHook = new(KeyCode.X, PlayerInputType.OnKeyHeld, nameof(HoldGrapplingHook));
        //public FixedKeybind ReleaseGrapplingHook = new(KeyCode.X, PlayerInputType.OnKeyUp, nameof(ReleaseGrapplingHook));

        public FixedKeybind SwitchCameraPosition = new(KeyCode.W, InputType.OnKeyDown, nameof(SwitchCameraPosition));
        public VariableKeybind QuickReset = new(KeyCode.X, new List<InputType>() { InputType.OnKeyDown, InputType.OnKeyHeldForTime }, .5f, nameof(QuickReset));


        public List<Keybind> Keybinds { get; private set; } = new();

        public void Init()
        {
            RegisterKeybind(Forward);
            RegisterKeybind(Back);
            RegisterKeybind(Right);
            RegisterKeybind(Left);

            RegisterKeybind(Jump);

            RegisterKeybind(Slide);

            RegisterKeybind(HoldCrouch);

            RegisterKeybind(Dash);

            RegisterKeybind(GrapplingHook);

            RegisterKeybind(SwitchCameraPosition);

            RegisterKeybind(QuickReset);
        }

        public void OnRenderRebindMenu()
        {
            foreach (var keybind in Keybinds)
            {
                keybind.OnRenderRebindMenu();
            }
        }

        public void RegisterKeybind(Keybind keybind)
        {
            Keybinds.Add(keybind);
            keybind.Init();
        }
    }
}