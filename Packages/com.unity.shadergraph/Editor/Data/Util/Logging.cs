using System;
using UnityEngine;

namespace UnityEditor.Graphing
{
    public class ConsoleLogHandler : ILogHandler
    {
        public void LogFormat(LogType logType, UnityEngine.Object context, string format, params object[] args)
        {
            var formatted = string.Format(format, args);
            Console.WriteLine("{0}:{1} - {2}", logType, context, formatted);
        }

        public void LogException(Exception exception, UnityEngine.Object context)
        {
            Console.WriteLine("{0} - {1}", context, exception);
        }
    }
}
