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
            CombatInputs = new();
            GeneralInputs = new();
            CameraHorizontalSensitivity = 3f;
            CameraVerticalSensitivity = 3f;

            return this;
        }
    }
}