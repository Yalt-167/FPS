using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;

namespace Inputs
{
    [Serializable]
    public sealed class MovementInputQuery : IInputQuery
    {
        public GroupKeybind Forward = new(KeyCode.Z, new InputType[] { InputType.OnKeyDown, InputType.OnKeyHeld }, nameof(Forward), true);
        public GroupKeybind Back = new(KeyCode.S, new InputType[] { InputType.OnKeyDown, InputType.OnKeyHeld }, nameof(Back), true);
        public GroupKeybind Right = new(KeyCode.D, new InputType[] { InputType.OnKeyDown, InputType.OnKeyHeld, InputType.OnKeyHeldForTime }, nameof(Right), true, .5f);
        public GroupKeybind Left = new(KeyCode.Q, new InputType[] { InputType.OnKeyDown, InputType.OnKeyHeld, InputType.OnKeyHeldForTime }, nameof(Left), true, .5f);
        public GroupKeybind Jump = new(KeyCode.Space, new InputType[] { InputType.OnKeyDown, InputType.OnKeyHeld, InputType.OnKeyUp }, nameof(Jump), true);

        public FixedKeybind Dash = new(KeyCode.Alpha9, InputType.OnKeyDown, nameof(Dash), true);
        public GroupKeybind Slide = new(KeyCode.LeftShift, new InputType[] { InputType.OnKeyDown, InputType.OnKeyHeld }, nameof(Slide), true);
        public GroupKeybind GrapplingHook = new(KeyCode.Space, new InputType[] { InputType.OnKeyDown, InputType.OnKeyHeld, InputType.OnKeyUp }, nameof(GrapplingHook), true);
        public VariableKeybind HoldCrouch = new(KeyCode.LeftControl, new List<InputType>() { InputType.OnKeyHeld, InputType.Toggle }, nameof(HoldCrouch), true);
        
        public FixedKeybind SwitchCameraPosition = new(KeyCode.W, InputType.OnKeyDown, nameof(SwitchCameraPosition), true);
        public VariableKeybind QuickReset = new(KeyCode.X, new List<InputType>() { InputType.OnKeyDown, InputType.OnKeyHeldForTime }, nameof(QuickReset), true, .5f);

        public List<Keybind> Keybinds { get; private set; } = new();
        public bool IsRebindingAKey { get; private set; }

        public void Init()
        {
            DebugUtility.LogMethodCall();
            DebugUtility.LogCallStack();

            RegisterKeybind(Forward);
            RegisterKeybind(Left);
            RegisterKeybind(Back);
            RegisterKeybind(Right);

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
            IsRebindingAKey = false;

            foreach (var keybind in Keybinds)
            {
                IsRebindingAKey |= keybind.OnRenderRebindMenu();
            }
        }

        public void RegisterKeybind(Keybind keybind)
        {
            Keybinds.Add(keybind);
            keybind.Init();
        }
    }
}