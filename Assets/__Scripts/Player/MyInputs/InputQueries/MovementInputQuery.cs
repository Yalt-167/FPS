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
        public GroupKeybind Forward = new(KeyCode.Z, new Dictionary<string, PlayerInputType>() { {"Initiate", PlayerInputType.OnKeyDown }, { "Hold", PlayerInputType.OnKeyHeld } }, nameof(Forward));
        public GroupKeybind Back = new(KeyCode.S, new Dictionary<string, PlayerInputType>() { {"Initiate", PlayerInputType.OnKeyDown }, { "Hold", PlayerInputType.OnKeyHeld } }, nameof(Back));
        public GroupKeybind Right = new(KeyCode.D, new Dictionary<string, PlayerInputType>() { {"Initiate", PlayerInputType.OnKeyDown }, { "Hold", PlayerInputType.OnKeyHeld }, { "HoldForTime", PlayerInputType.OnHeldForTime } }, nameof(Right));
        public GroupKeybind Left = new(KeyCode.Q, new Dictionary<string, PlayerInputType>() { {"Initiate", PlayerInputType.OnKeyDown }, { "Hold", PlayerInputType.OnKeyHeld }, { "HoldForTime", PlayerInputType.OnHeldForTime } }, nameof(Left));
        public GroupKeybind Jump = new(KeyCode.Space, new Dictionary<string, PlayerInputType>() { {"Initiate", PlayerInputType.OnKeyDown }, { "Hold", PlayerInputType.OnKeyHeld }, { "Release", PlayerInputType.OnKeyUp } }, nameof(Jump));
        public GroupKeybind Slide = new(KeyCode.LeftShift, new Dictionary<string, PlayerInputType>() { {"Initiate", PlayerInputType.OnKeyDown }, { "Hold", PlayerInputType.OnKeyHeld }}, nameof(Slide));
        public GroupKeybind GrapplingHook = new(KeyCode.Space, new Dictionary<string, PlayerInputType>() { {"Initiate", PlayerInputType.OnKeyDown }, { "Hold", PlayerInputType.OnKeyHeld }, { "Release", PlayerInputType.OnKeyUp } }, nameof(GrapplingHook));

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

        public VariableKeybind HoldCrouch = new(KeyCode.LeftControl, new List<PlayerInputType>() { PlayerInputType.OnKeyHeld, PlayerInputType.Toggle }, nameof(HoldCrouch));

        public FixedKeybind Dash = new(KeyCode.Alpha9, PlayerInputType.OnKeyDown, nameof(Dash));

        //public FixedKeybind InitiateGrapplingHook = new(KeyCode.X, PlayerInputType.OnKeyDown, nameof(InitiateGrapplingHook));
        //public FixedKeybind HoldGrapplingHook = new(KeyCode.X, PlayerInputType.OnKeyHeld, nameof(HoldGrapplingHook));
        //public FixedKeybind ReleaseGrapplingHook = new(KeyCode.X, PlayerInputType.OnKeyUp, nameof(ReleaseGrapplingHook));

        public FixedKeybind SwitchCameraPosition = new(KeyCode.W, PlayerInputType.OnKeyDown, nameof(SwitchCameraPosition));
        public VariableKeybind QuickReset = new(KeyCode.X, new List<PlayerInputType>() { PlayerInputType.OnKeyDown, PlayerInputType.OnHeldForTime }, .5f, nameof(QuickReset));


        public List<Keybind> Keybinds { get; private set; } = new();

        public bool DoRenderRebindMenu { get; private set; }

        public void Init()
        {
            //InitiateForward.Init(this);
            Forward.Init(this);

            //InitiateBack.Init(this);
            Back.Init(this);

            //InitiateRight.Init(this);
            Right.Init(this);
            //HoldRightForTime.Init(this);

            //InitiateLeft.Init(this);
            Left.Init(this);
            //HoldLeftForTime.Init(this);

            //InitiateJump.Init(this);
            //HoldJump.Init(this);
            //InterruptJump.Init(this);
            Jump.Init(this);

            //InitiateSlide.Init(this);
            //HoldSlide.Init(this);
            Slide.Init(this);
            HoldCrouch.Init(this);
            Dash.Init(this);

            //InitiateGrapplingHook.Init(this);
            //HoldGrapplingHook.Init(this);
            //ReleaseGrapplingHook.Init(this);
            GrapplingHook.Init(this);

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