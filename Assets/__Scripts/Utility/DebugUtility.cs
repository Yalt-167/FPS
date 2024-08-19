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
        Debug.Log($"[Log Calls]: {new StackTrace().GetFrame(1).GetMethod().Name}");
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
