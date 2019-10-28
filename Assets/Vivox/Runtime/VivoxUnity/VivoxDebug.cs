/*
Copyright (c) 2014-2018 by Mercer Road Corp

Permission to use, copy, modify or distribute this software in binary or source form
for any purpose is allowed only under explicit prior consent in writing from Mercer Road Corp

THE SOFTWARE IS PROVIDED "AS IS" AND MERCER ROAD CORP DISCLAIMS
ALL WARRANTIES WITH REGARD TO THIS SOFTWARE INCLUDING ALL IMPLIED
WARRANTIES OF MERCHANTABILITY AND FITNESS. IN NO EVENT SHALL MERCER ROAD CORP
BE LIABLE FOR ANY SPECIAL, DIRECT, INDIRECT, OR CONSEQUENTIAL
DAMAGES OR ANY DAMAGES WHATSOEVER RESULTING FROM LOSS OF USE, DATA OR
PROFITS, WHETHER IN AN ACTION OF CONTRACT, NEGLIGENCE OR OTHER TORTIOUS
ACTION, ARISING OUT OF OR IN CONNECTION WITH THE USE OR PERFORMANCE OF THIS
SOFTWARE.
*/

using System.Diagnostics;
using SystemDebug = System.Diagnostics.Debug;
#if UNITY_5_3_OR_NEWER
using UnityDebug = UnityEngine.Debug;
#endif

namespace VivoxUnity
{
    public class VivoxDebug
    {
        private static VivoxDebug _instance;

        /// <summary>Where to put the logs: 0 = both, 1 = unity console only, 2 = visual studio console only</summary>
        public int debugLocation;

        /// <summary>Setting this will tell VivoxUnity if we should rethrow an exception that has occured internally</summary>
        public bool throwInternalExcepetions = true;

        public static VivoxDebug Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new VivoxDebug();
                    VivoxCoreInstancePINVOKE.SWIGLogHelper.isLogging = true;
                }
                return _instance;
            }
            set
            {
                _instance = value;
            }
        }

        ~VivoxDebug()
        {
            VivoxCoreInstancePINVOKE.SWIGLogHelper.isLogging = false;
        }

        /// <summary>
        /// Debugs the message.
        /// VivoxUnity uses this to catch exceptions thrown internally for logging.
        /// </summary>
        /// <param name="message">Message.</param>
        internal void VxExceptionMessage(string message)
        {
            string callerMethodName = new StackFrame(1).GetMethod().Name;
            string newMessage = $"{callerMethodName}: {message}";

            DebugMessage(newMessage, vx_log_level.log_error);
        }

        /// <summary>
        /// Debugs the message.
        /// log_none (-1) will throw away your message.
        /// log_warning (1) will display as a warning.
        /// log_error (0) will display as an error.
        /// log_info (2), log_debug (3), log_trace (4), log_all (5) will display as a normal debug.
        /// </summary>
        /// <param name="message">Message.</param>
        /// <param name="severity">Defaults to 2.</param>
        public virtual void DebugMessage(object message, vx_log_level severity = vx_log_level.log_debug)
        {
            // Lets attempt to log what was sent
            try
            {
                if (severity == vx_log_level.log_none) return;
                if (severity != vx_log_level.log_error && severity != vx_log_level.log_warning)
                {
#if UNITY_5_3_OR_NEWER
                    if (debugLocation != 2)
                        UnityDebug.Log(message);
#endif
                    if (debugLocation != 1)
                        SystemDebug.WriteLine(message.ToString(), TraceLevel.Info.ToString());
                }
                if (severity == vx_log_level.log_warning)
                {
#if UNITY_5_3_OR_NEWER

                    if (debugLocation != 2)
                        UnityDebug.LogWarning(message);
#endif
                    if (debugLocation != 1)
                        SystemDebug.WriteLine(message.ToString(), TraceLevel.Warning.ToString());
                }
                if (severity == vx_log_level.log_error)
                {
#if UNITY_5_3_OR_NEWER

                    if (debugLocation != 2)
                        UnityDebug.LogError(message);
#endif
                    if (debugLocation != 1)
                        SystemDebug.WriteLine(message.ToString(), TraceLevel.Error.ToString());
                }
            }
            catch (System.Exception e)
            {
#if UNITY_5_3_OR_NEWER

                if (debugLocation != 2)
                    UnityDebug.LogError(message);
#endif
                if (debugLocation != 1)
                    SystemDebug.WriteLine(e, TraceLevel.Error.ToString());
            }
        }
    }
}
