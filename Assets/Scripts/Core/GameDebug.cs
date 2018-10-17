using System;
using UnityEngine;

//
// Logging of messages
//
// There are three different types of messages:
//
// Debug.Log/Warn/Error coming from unity (or code, e.g. packages, not using GameDebug)
//    These get caught here and sent onto the console and into our log file
// GameDebug.Log/Warn/Error coming from game
//    These gets sent onto the console and into our log file
//    *IF* we are in editor, they are also sent to Debug.* so they show up in editor Console window
// Console.Write
//    Only used for things that should not be logged. Typically reponses to user commands. Only shown on Console.
//

public static class GameDebug
{
    static System.IO.StreamWriter logFile = null;

    static bool forwardToDebug = true;
    public static void Init(string logfilePath, string logBaseName)
    {
        forwardToDebug = Application.isEditor;
        Application.logMessageReceived += LogCallback;

        // Try creating logName; attempt a number of suffixxes
        string name = "";
        for (var i = 0; i < 10; i++)
        {
            name = logBaseName + (i == 0 ? "" : "_" + i) + ".log";
            try
            {
                logFile = System.IO.File.CreateText(logfilePath + "/" + name);
                logFile.AutoFlush = true;
                break;
            }
            catch
            {
                name = "<none>";
            }
        }
        GameDebug.Log("GameDebug initialized. Logging to " + logfilePath + "/" + name);
    }

    public static void Shutdown()
    {
        Application.logMessageReceived -= LogCallback;
        if (logFile != null)
            logFile.Close();
        logFile = null;
    }

    static void LogCallback(string message, string stack, LogType logtype)
    {
        switch (logtype)
        {
            default:
            case LogType.Log:
                GameDebug._Log(message);
                break;
            case LogType.Warning:
                GameDebug._LogWarning(message);
                break;
            case LogType.Error:
                GameDebug._LogError(message);
                break;
        }
    }

    public static void Log(string message)
    {
        if (forwardToDebug)
            Debug.Log(message);
        else
            _Log(message);
    }

    static void _Log(string message)
    {
        Console.Write(Time.frameCount + ": " + message);
        if (logFile != null)
            logFile.WriteLine(Time.frameCount + ": " + message + "\n");
    }

    public static void LogError(string message)
    {
        if (forwardToDebug)
            Debug.LogError(message);
        else
            _LogError(message);
    }

    static void _LogError(string message)
    {
        Console.Write(Time.frameCount + ": [ERR] " + message);
        if (logFile != null)
            logFile.WriteLine("[ERR] " + message + "\n");
    }

    public static void LogWarning(string message)
    {
        if (forwardToDebug)
            Debug.LogWarning(message);
        else
            _LogWarning(message);
    }

    static void _LogWarning(string message)
    {
        Console.Write(Time.frameCount + ": [WARN] " + message);
        if (logFile != null)
            logFile.WriteLine("[WARN] " + message + "\n");
    }

    public static void Assert(bool condition)
    {
        if (!condition)
            throw new ApplicationException("GAME ASSERT FAILED");
    }

    public static void Assert(bool condition, string msg)
    {
        if (!condition)
            throw new ApplicationException("GAME ASSERT FAILED : " + msg);
    }

    public static void Assert<T>(bool condition, string format, T arg1)
    {
        if (!condition)
            throw new ApplicationException("GAME ASSERT FAILED : " + string.Format(format, arg1));
    }

    public static void Assert<T1, T2>(bool condition, string format, T1 arg1, T2 arg2)
    {
        if (!condition)
            throw new ApplicationException("GAME ASSERT FAILED : " + string.Format(format, arg1, arg2));
    }

    public static void Assert<T1, T2, T3>(bool condition, string format, T1 arg1, T2 arg2, T3 arg3)
    {
        if (!condition)
            throw new ApplicationException("GAME ASSERT FAILED : " + string.Format(format, arg1, arg2, arg3));
    }

    public static void Assert<T1, T2, T3, T4>(bool condition, string format, T1 arg1, T2 arg2, T3 arg3, T4 arg4)
    {
        if (!condition)
            throw new ApplicationException("GAME ASSERT FAILED : " + string.Format(format, arg1, arg2, arg3, arg4));
    }

    public static void Assert<T1, T2, T3, T4, T5>(bool condition, string format, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5)
    {
        if (!condition)
            throw new ApplicationException("GAME ASSERT FAILED : " + string.Format(format, arg1, arg2, arg3, arg4, arg5));
    }
}
