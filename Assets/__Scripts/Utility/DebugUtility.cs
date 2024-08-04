using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using UnityEngine;
using Debug = UnityEngine.Debug;

public static class DebugUtility
{
    public static void LogMethodCall()
    {
        Debug.Log($"[Log Calls]: {new StackTrace().GetFrame(1).GetMethod().Name}");
    }
}
