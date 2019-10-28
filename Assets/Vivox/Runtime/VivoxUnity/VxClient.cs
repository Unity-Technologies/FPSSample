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

using System;
using System.Collections.Generic;
using VivoxUnity.Private;

namespace VivoxUnity
{
    public class VxClient : IDisposable
    {
        private static VxClient _instance;
        private readonly Dictionary<string, AsyncResult<vx_resp_base_t>> _pendingRequests = new Dictionary<string, AsyncResult<vx_resp_base_t>>();
        private long _nextRequestId = 1;
        private int _startCount = 0;
        /// <summary>
        ///   three letter appId; Do not set this value, or contact your Vivox representative for more information. This should be set to UNI
        /// </summary>
        public const string appId = "UNI"; // Please do not change this value

        public VivoxDebug vivoxDebug;


        private VxClient() {
        }

        public static VxClient Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new VxClient();
                }
                return _instance;
            }
        }

        public bool Started { get { return _startCount > 0;  } }

        public delegate void HandleEventMessage(vx_evt_base_t eventMessage);
        public event HandleEventMessage EventMessageReceived;

        /// <param name="logLevel">Severity level of logs: -1 = no logging, 0 = errors only, 1 = warnings, 2 = info, 3 = debug, 4 = trace, 5 = log all</param>
        /// <param name="debugLocation">Optional: Where to put the logs: 0 = both, 1 = unity console only, 2 = visual studio console only</param>
        [Obsolete("This method is deprecated. Please use Start(int debugLocation = 0, vx_sdk_config_t config = null) method signiture.  " +
            "logLevel can be set in vx_sdk_config_t.initial_log_level = (vx_log_level)1.  debugLocation can be set and changed at anytime in VivoxDebug.Instance.debugLocation")]
        public void Start(int logLevel, int debugLocation = 0)
        {
            vx_sdk_config_t config = new vx_sdk_config_t();
            config.initial_log_level = (vx_log_level)logLevel;
            VivoxDebug.Instance.debugLocation = debugLocation;
            Start(config);
        }


        /// <param name="config">Optional: config to set on initialize.</param>
        public void Start(vx_sdk_config_t config = null)
        {
            if (_startCount > 0)
            {
                ++_startCount;
                return;
            }
            config = config == null ? new vx_sdk_config_t() : config;

            config.app_id = appId;

            int status = VivoxCoreInstance.Initialize(config);
            if (status != 0)
                throw new VivoxApiException(status);

            MessagePump.Instance.MainLoopRun += InstanceOnMainLoopRun;

            ++_startCount;
        }

        private void InstanceOnMainLoopRun(ref bool didWork)
        {
            if (!Started)
                return;
            for (;;)
            {
                vx_message_base_t m = VivoxUnity.Helper.NextMessage();

                if (m == null)
                    break;
                didWork = true;
                if (m.type == vx_message_type.msg_event)
                {
                    EventMessageReceived?.Invoke((vx_evt_base_t)m);
                }
                else if (m.type == vx_message_type.msg_response)
                {
                    var r = (vx_resp_base_t)m;
                    string key = r.request.cookie;
                    AsyncResult<vx_resp_base_t> result = null;
                    lock (_pendingRequests)
                    {
                        if (_pendingRequests.ContainsKey(key))
                        {
                            result = _pendingRequests[key];
                            _pendingRequests.Remove(key);
                        }
                    }
                    result?.SetComplete(r);
                }
            }
        }

        public void Stop()
        {
            if (_startCount <= 0)
                return;
            --_startCount;
            if (_startCount != 0)
                return;
            MessagePump.Instance.MainLoopRun -= InstanceOnMainLoopRun;
            VivoxCoreInstance.Uninitialize();
        }

        public void Cleanup()
        {
            MessagePump.Instance.MainLoopRun -= InstanceOnMainLoopRun;
            VivoxCoreInstance.Uninitialize();
            lock (_pendingRequests)
            {
                _pendingRequests.Clear();
            }
            _startCount = 0;
        }

        public IAsyncResult BeginIssueRequest(vx_req_base_t request, AsyncCallback callback)
        {
            if (request == null)
                throw new ArgumentNullException();
            if (!Started)
                throw new InvalidOperationException();
            string requestId = $"{_nextRequestId++}";
            request.cookie = requestId;
            var result = new AsyncResult<vx_resp_base_t>(callback) {AsyncState = requestId};
            lock (_pendingRequests)
            {
                _pendingRequests[requestId] = result;
            }
            var status = VivoxCoreInstance.IssueRequest(request);
            if (status != 0)
            {
                lock(_pendingRequests) {
                    _pendingRequests.Remove(requestId);
                }
                throw new VivoxApiException(status);
            }
            return result;
        }

        public vx_resp_base_t EndIssueRequest(IAsyncResult result)
        {
            if (result == null)
                throw new ArgumentNullException();
            if (!result.IsCompleted)
                throw new InvalidOperationException();
            var tresult = result as AsyncResult<vx_resp_base_t>;
            if (tresult == null)
                throw new InvalidCastException();
            if(tresult.Result.return_code == 1)
            {
                throw new VivoxApiException(tresult.Result.status_code, tresult.Result.request.cookie);
            }
            return tresult.Result;
        }

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                }
                disposedValue = true;
            }
        }

        // TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
        // ~VxClient() {
        //   // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
        //   Dispose(false);
        // }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
            // TODO: uncomment the following line if the finalizer is overridden above.
            // GC.SuppressFinalize(this);
        }
        #endregion

        public static string GetLoginToken(string issuer, TimeSpan expiration, string userUri, string key)
        {
            return Helper.GetLoginToken(issuer, expiration, userUri, key);
        }
        public static string GetJoinToken(string issuer, TimeSpan expiration, string userUri, string conferenceUri, string key)
        {
            return Helper.GetJoinToken(issuer, expiration, userUri, conferenceUri, key);
        }
        public static string GetMuteForAllToken(string issuer, TimeSpan expiration, string subject, string fromUserUri, string conferenceUri, string key)
        {
            return Helper.GetMuteForAllToken(issuer, expiration, fromUserUri, subject, conferenceUri, key);
        }
        public static string GetRandomUserId(string prefix)
        {
            return Helper.GetRandomUserId(prefix);
        }
        public static string GetRandomUserIdEx(string prefix, string issuer)
        {
            return Helper.GetRandomUserIdEx(prefix, issuer);
        }
        public static string GetRandomChannelUri(string prefix, string realm)
        {
            return Helper.GetRandomChannelUri(prefix, realm);
        }

        public static string GetVersion()
        {
            return VivoxCoreInstance.vx_get_sdk_version_info();
        }
    }
}
