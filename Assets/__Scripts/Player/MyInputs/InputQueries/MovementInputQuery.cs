using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;

namespace Inputs
{
    [Serializable]
    public sealed class MovementInputQuery : IInputQuery
    {
        public GroupKeybind Forward = new(KeyCode.Z, new InputType[] { InputType.OnKeyDown, InputType.OnKeyHeld }, nameof(Forward));
        public GroupKeybind Back = new(KeyCode.S, new InputType[] { InputType.OnKeyDown, InputType.OnKeyHeld }, nameof(Back));
        public GroupKeybind Right = new(KeyCode.D, new InputType[] { InputType.OnKeyDown, InputType.OnKeyHeld, InputType.OnKeyHeldForTime }, nameof(Right));
        public GroupKeybind Left = new(KeyCode.Q, new InputType[] { InputType.OnKeyDown, InputType.OnKeyHeld, InputType.OnKeyHeldForTime }, nameof(Left));
        public GroupKeybind Jump = new(KeyCode.Space, new InputType[] { InputType.OnKeyDown, InputType.OnKeyHeld, InputType.OnKeyUp }, nameof(Jump));

        public FixedKeybind Dash = new(KeyCode.Alpha9, InputType.OnKeyDown, nameof(Dash));
        public GroupKeybind Slide = new(KeyCode.LeftShift, new InputType[] { InputType.OnKeyDown, InputType.OnKeyHeld }, nameof(Slide));
        public GroupKeybind GrapplingHook = new(KeyCode.Space, new InputType[] { InputType.OnKeyDown, InputType.OnKeyHeld, InputType.OnKeyUp }, nameof(GrapplingHook));
        public VariableKeybind HoldCrouch = new(KeyCode.LeftControl, new List<InputType>() { InputType.OnKeyHeld, InputType.Toggle }, nameof(HoldCrouch));
        
        public FixedKeybind SwitchCameraPosition = new(KeyCode.W, InputType.OnKeyDown, nameof(SwitchCameraPosition));
        public VariableKeybind QuickReset = new(KeyCode.X, new List<InputType>() { InputType.OnKeyDown, InputType.OnKeyHeldForTime }, .5f, nameof(QuickReset));

        public List<Keybind> Keybinds { get; private set; } = new();
        public bool IsRebindingAKey { get; private set; }

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