using Inputs;

namespace SaveAndLoad
{
    [System.Serializable]
    public struct KeybindsSave : IAmSomethingToSave
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

        public IAmSomethingToSave Init()
        {
            UnityEngine.Debug.Log(CameraHorizontalSensitivity);
            MovementInputs.Init();
            CombatInputs.Init();
            GeneralInputs.Init();

            return this;
        }
    }
}