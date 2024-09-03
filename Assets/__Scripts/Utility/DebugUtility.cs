using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Text;
using UnityEngine;
using Debug = UnityEngine.Debug;

public static class DebugUtility
{
    public static void LogMethodCall()
    {
        Debug.Log($"[DebugUtility::Log Calls]: {new StackTrace().GetFrame(1).GetMethod().Name}");
    }

    public static void LogCallStack()
    {
        var stackTrace = new StackTrace(skipFrames: 1);

        var upperBound = 5 > stackTrace.FrameCount ? stackTrace.FrameCount : 5;
        for (int i = 0; i < upperBound; i++)
        {
            StackFrame frame = stackTrace.GetFrame(i);
            var method = frame.GetMethod();

            Debug.Log($"[DebugUtility::Log Call Stack Frame {i}]: {method.DeclaringType.FullName}::{method.Name}");
        }
    }

    public static void PrintIterable(IEnumerable iterable)
    {
        var stringBuilder = new StringBuilder();

        stringBuilder.Append("[ ");

        var isFirst = true;
        foreach (var item in iterable)
        {
            stringBuilder.Append(isFirst ? $"{item?.ToString()}" : $", {item?.ToString()}");
            isFirst = false;
        }

        stringBuilder.Append(" ]");

        Debug.Log(stringBuilder.ToString());
    }
}
