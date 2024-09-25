namespace MyDebug
{
    public static class DebugUtility
    {
        public static void LogMethodCall()
        {
            UnityEngine.Debug.Log($"[DebugUtility::Log Calls]: {new System.Diagnostics.StackTrace().GetFrame(1).GetMethod().Name}");
        }

        public static void LogCallStack(int depth = 5)
        {
            var stackTrace = new System.Diagnostics.StackTrace(skipFrames: 1);

            var upperBound = depth > stackTrace.FrameCount ? stackTrace.FrameCount : depth; // basically upperBound = depth;
            for (int i = 0; i < upperBound; i++)
            {
                var method = stackTrace.GetFrame(i).GetMethod();

                UnityEngine.Debug.Log($"[DebugUtility::Log Call Stack Frame {i}]: {method.DeclaringType.FullName}::{method.Name}");
            }
        }

        public static void PrintIterable(System.Collections.IEnumerable iterable)
        {
            var stringBuilder = new System.Text.StringBuilder();

            _ = stringBuilder.Append("[ ");

            var isFirst = true;
            foreach (var item in iterable)
            {
                _ = stringBuilder.Append(isFirst ? $"{item?.ToString()}" : $", {item?.ToString()}");
                isFirst = false;
            }

            _ = stringBuilder.Append(" ]");

            UnityEngine.Debug.Log(stringBuilder.ToString());
        }
    }

}