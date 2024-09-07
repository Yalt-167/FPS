using Inputs;
using SaveAndLoad;
using System;

namespace SaveAndLoad
{
    [Serializable]
    public struct InputManagerSaveablePart : IAmSomethingToSave
    {
        public MovementInputQuery MovementInputs;
        public CombatInputQuery CombatInputs;
        public GeneralInputQuery GeneralInputs;
        public float CameraHorizontalSensitivity;
        public float CameraVerticalSensitivity;

        public IAmSomethingToSave SetDefault()
        {
            MovementInputs = new();
            MovementInputs.Init();
            CombatInputs = new();
            CombatInputs.Init();
            GeneralInputs = new();
            GeneralInputs.Init();
            CameraHorizontalSensitivity = 3f;
            CameraVerticalSensitivity = 3f;

            return this;
        }
    }
}