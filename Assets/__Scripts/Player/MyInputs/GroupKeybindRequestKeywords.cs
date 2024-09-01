namespace Inputs
{
    public static class GroupKeybindRequestKeywords
    {
        public static string GetRelevantNameFromInputType(InputType inputType)
        {
            return inputType switch
            {
                InputType.OnKeyDown => Initiate,
                InputType.OnKeyUp => Release,
                InputType.OnKeyHeld => Hold,
                InputType.OnKeyHeldForTime => HoldForTime,
                InputType.Toggle => Toggle,
                _ => throw new System.Exception($"The input type {inputType} does not exist")
            };

        }

        public static readonly string Initiate = nameof(Initiate);
        public static readonly string Release = nameof(Release);
        public static readonly string Hold = nameof(Hold);
        public static readonly string HoldForTime = nameof(HoldForTime);
        public static readonly string Toggle = nameof(Toggle);
    }
}