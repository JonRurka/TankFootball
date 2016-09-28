using System;
using UnityEngine;
using System.Collections;

public static class SafeDebug {

    public static void Log(object message) {
        string stackTrace = StackTraceUtility.ExtractStackTrace();
        TaskQueue.QueueMain(()=> Debug.Log(message.ToString() + "\n" + stackTrace));
    }

    public static void LogWarning(object message) {
        string stackTrace = StackTraceUtility.ExtractStackTrace();
        TaskQueue.QueueMain(() => Debug.LogWarning(message.ToString() + "\n" + stackTrace));
    }

    public static void LogError(object message, Exception e = null) {
        string stackTrace = StackTraceUtility.ExtractStackTrace();
        string ErrorLocation = string.Empty;
#if UNITY_EDITOR
        if (e != null)
        {
            System.Diagnostics.StackTrace trace = new System.Diagnostics.StackTrace(e, true);
            System.Diagnostics.StackFrame frame = trace.GetFrame(0);
            ErrorLocation = "\n" + frame.GetFileName() + "." + frame.GetMethod() + ": " + frame.GetFileLineNumber();
        }
#endif
        TaskQueue.QueueMain(() => Debug.LogError(message.ToString() + "\n" + stackTrace));
    }

    public static void LogException(Exception message) {
        TaskQueue.QueueMain(() => {
            Debug.LogException(message);
        });
    }
}
