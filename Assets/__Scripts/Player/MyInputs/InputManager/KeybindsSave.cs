using Inputs;
using SaveAndLoad;
using System;

namespace SaveAndLoad
{
    [Serializable]
    public struct KeybindsSave : IAmSomethingToSave
    {
        public MovementInputQuery MovementInputs;
        public CombatInputQuery CombatInputs;
        public GeneralInputQuery GeneralInputs;
        public float CameraHorizontalSensitivity;
        public float CameraVerticalSensitivity;

        public IAmSomethingToSave SetDefault()
        {
            UnityEngine.Debug.Log("SetDefault");
            MovementInputs = new();
            CombatInputs = new();
            GeneralInputs = new();
            CameraHorizontalSensitivity = 3f;
            CameraVerticalSensitivity = 3f;

            return this;
        }

        public readonly IAmSomethingToSave Init()
        {
            UnityEngine.Debug.Log(CameraHorizontalSensitivity);
            MovementInputs.Init();
            CombatInputs.Init();
            GeneralInputs.Init();

            return this;
        }
    }
}