using System;
using System.Collections;
using System.Collections.Generic;

using System.Diagnostics;
using System.Reflection;
using System.Text;


namespace MyDebug
{
    public static class DebugUtility
    {
        public static void LogMethodCall()
        {
            UnityEngine.Debug.Log($"[DebugUtility::Log Calls]: {new StackTrace().GetFrame(1).GetMethod().Name}");
        }

        public static void LogCallStack(int depth = 5)
        {
            var stackTrace = new StackTrace(skipFrames: 1);

            var upperBound = depth > stackTrace.FrameCount ? stackTrace.FrameCount : depth; // basically upperBound = depth;
            for (int i = 0; i < upperBound; i++)
            {
                var method = stackTrace.GetFrame(i).GetMethod();

                UnityEngine.Debug.Log($"[DebugUtility::Log Call Stack Frame {i}]: {method.DeclaringType.FullName}::{method.Name}");
            }
        }

        public static void PrintIterable(IEnumerable iterable)
        {
            var stringBuilder = new StringBuilder();

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