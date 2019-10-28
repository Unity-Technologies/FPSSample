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
using System.ComponentModel;
using System.Diagnostics;
using VivoxUnity.Common;

namespace VivoxUnity.Private
{
    internal class ChannelSession : IChannelSession
    {
        #region Member Variables

        private readonly LoginSession _loginSession;
        private readonly string _sessionHandle;
        private bool _typing;

        private readonly ReadWriteDictionary<string, IParticipant, ChannelParticipant> _participants =
            new ReadWriteDictionary<string, IParticipant, ChannelParticipant>();

        private readonly ReadWriteQueue<IChannelTextMessage> _messageLog = new ReadWriteQueue<IChannelTextMessage>();
        private readonly ReadWriteQueue<ISessionArchiveMessage> _sessionArchive = new ReadWriteQueue<ISessionArchiveMessage>();

        private ArchiveQueryResult _sessionArchiveResult = new ArchiveQueryResult();

        private ConnectionState _audioState;
        private ConnectionState _textState;
        private int _nextTextId = 0;
        private bool _deleted;

        #endregion

        #region Events

        public event PropertyChangedEventHandler PropertyChanged;

        #endregion

        #region Helpers

        bool AlreadyDone(bool connect, ConnectionState state)
        {
            if (connect)
            {
                return state == ConnectionState.Connected || state == ConnectionState.Connecting;
            }
            else
            {
                return state == ConnectionState.Disconnected || state == ConnectionState.Disconnecting;
            }
        }

        void AssertSessionNotDeleted()
        {
            if (_deleted)
                throw new InvalidOperationException($"{GetType().Name}: Session has been deleted");
        }

        #endregion

        public ChannelSession(LoginSession loginSession, ChannelId channelId, string groupId)
        {
            if (loginSession == null) throw new ArgumentNullException(nameof(loginSession));
            if (ChannelId.IsNullOrEmpty(channelId)) throw new ArgumentNullException(nameof(channelId));
            _loginSession = loginSession;
            Key = channelId;
            GroupId = groupId;
            _sessionHandle = $"{loginSession.AccountHandle}_{channelId}";
            VxClient.Instance.EventMessageReceived += InstanceOnEventMessageReceived;
            _loginSession.PropertyChanged += (sender, args) =>
            {
                if (args.PropertyName == nameof(_loginSession.TransmittingSession))
                {
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsTransmittingSession)));
                }
            };
        }

        #region Handle Events Messages

        private void InstanceOnEventMessageReceived(vx_evt_base_t eventMessage)
        {
            if (_deleted) return;
            switch ((vx_event_type)eventMessage.type)
            {
                case vx_event_type.evt_participant_added:
                    HandleParticipantAdded(eventMessage);
                    break;
                case vx_event_type.evt_participant_removed:
                    HandleParticipantRemoved(eventMessage);
                    break;
                case vx_event_type.evt_participant_updated:
                    HandleParticipantUpdated(eventMessage);
                    break;
                case vx_event_type.evt_media_stream_updated:
                    HandleMediaStreamUpdated(eventMessage);
                    break;
                case vx_event_type.evt_text_stream_updated:
                    HandleTextStreamUpdated(eventMessage);
                    break;
                case vx_event_type.evt_session_removed:
                    HandleSessionRemoved(eventMessage);
                    break;
                case vx_event_type.evt_message:
                    HandleSessionMessage(eventMessage);
                    break;
                case vx_event_type.evt_session_archive_message:
                    HandleSessionArchiveMessage(eventMessage);
                    break;
                case vx_event_type.evt_session_archive_query_end:
                    HandleSessionArchiveQueryEnd(eventMessage);
                    break;
            }
        }

        private void HandleParticipantAdded(vx_evt_base_t eventMessage)
        {
            vx_evt_participant_added_t evt = eventMessage;
            Debug.Assert(evt != null);
            if (evt.session_handle != _sessionHandle) return;
            _participants[evt.participant_uri] = new ChannelParticipant(this, evt);
        }


        private void HandleParticipantRemoved(vx_evt_base_t eventMessage)
        {
            vx_evt_participant_removed_t evt = eventMessage;
            Debug.Assert(evt != null);
            if (evt.session_handle != _sessionHandle) return;
            _participants.Remove(evt.participant_uri);
        }


        private void HandleParticipantUpdated(vx_evt_base_t eventMessage)
        {
            vx_evt_participant_updated_t evt = eventMessage;
            Debug.Assert(evt != null);
            if (evt.session_handle != _sessionHandle) return;
            if (_participants.ContainsKey(evt.participant_uri))
            {
                ChannelParticipant p = _participants[evt.participant_uri] as ChannelParticipant;
                Debug.Assert(p != null);
                p.IsMutedForAll = evt.is_moderator_muted != 0;
                p.SpeechDetected = evt.is_speaking != 0;
                p.InAudio = (evt.active_media & 0x1) == 0x1;
                p.InText = (evt.active_media & 0x2) == 0x2;
                p.AudioEnergy = evt.energy;
                p._internalVolumeAdjustment = evt.volume;
                p._internalMute = evt.is_muted_for_me != 0;
            }
        }

        private void HandleMediaStreamUpdated(vx_evt_base_t eventMessage)
        {
            vx_evt_media_stream_updated_t evt = eventMessage;
            Debug.Assert(evt != null);
            if (evt.session_handle != _sessionHandle) return;

            if (evt.state == vx_session_media_state.session_media_connected)
            {
                AudioState = ConnectionState.Connected;
            }
            else if (evt.state == vx_session_media_state.session_media_disconnected)
            {
                AudioState = ConnectionState.Disconnected;
            }
        }

        private void HandleTextStreamUpdated(vx_evt_base_t eventMessage)
        {
            vx_evt_text_stream_updated_t evt = eventMessage;
            Debug.Assert(evt != null);
            if (evt.session_handle != _sessionHandle) return;
            if (evt.state == vx_session_text_state.session_text_connected)
            {
                TextState = ConnectionState.Connected;
            }
            else if (evt.state == vx_session_text_state.session_text_disconnected)
            {
                TextState = ConnectionState.Disconnected;
            }
        }

        private void HandleSessionRemoved(vx_evt_base_t eventMessage)
        {
            vx_evt_session_removed_t evt = eventMessage;
            Debug.Assert(evt != null);
            if (evt.session_handle != _sessionHandle) return;
            AudioState = ConnectionState.Disconnected;
            TextState = ConnectionState.Disconnected;
            _participants.Clear();
            _typing = false;
            _messageLog.Clear();
        }

        private void HandleSessionMessage(vx_evt_base_t eventMessage)
        {
            vx_evt_message_t evt = eventMessage;
            Debug.Assert(evt != null);
            if (evt.session_handle != _sessionHandle) return;
            var p = _participants[evt.participant_uri] as ChannelParticipant;

            var message = new ChannelTextMessage()
            {
                Sender = p != null ? p.Account : new AccountId(evt.participant_uri),
                Message = evt.message_body,
                ReceivedTime = DateTime.Now,
                Key = _nextTextId++.ToString("D8"),
                ApplicationStanzaBody = evt.application_stanza_body,
                ApplicationStanzaNamespace = evt.application_stanza_namespace,
                Language = evt.language,
                ChannelSession = this,
                FromSelf = (evt.is_current_user != 0)
            };
            _messageLog.Enqueue(message);
        }

        private void HandleSessionArchiveMessage(vx_evt_base_t eventMessage)
        {
            vx_evt_session_archive_message_t evt = eventMessage;
            Debug.Assert(evt != null);
            if (evt.session_handle != _sessionHandle) return;

            DateTime parsedReceivedTime;

            if (!DateTime.TryParse(evt.time_stamp, out parsedReceivedTime))
            {
                VivoxDebug.Instance.DebugMessage($"{GetType().Name}: {eventMessage.GetType().Name} invalid message: Bad time format", vx_log_level.log_error);
                Debug.Assert(false);
                return;
            }
            ChannelParticipant p = _participants[evt.participant_uri] as ChannelParticipant;

            var message = new SessionArchiveMessage()
            {
                ChannelSession = this,
                Key = evt.message_id,
                MessageId = evt.message_id,
                QueryId = evt.query_id,
                ReceivedTime = parsedReceivedTime,
                Sender = p != null ? p.Account : new AccountId(evt.participant_uri),
                Message = evt.message_body,
                FromSelf = (evt.is_current_user != 0),
                Language = evt.language
            };
            _sessionArchive.Enqueue(message);
        }

        private void HandleSessionArchiveQueryEnd(vx_evt_base_t eventMessage)
        {
            vx_evt_session_archive_query_end_t evt = eventMessage;
            Debug.Assert(evt != null);
            if (evt.session_handle != _sessionHandle) return;
            if (_sessionArchiveResult.QueryId != evt.query_id || !_sessionArchiveResult.Running) return;

            _sessionArchiveResult = new ArchiveQueryResult(evt);
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SessionArchiveResult)));
        }
        #endregion

        #region IChannelSession Implementation

        public IAsyncResult BeginConnect(bool connectAudio, bool connectText, TransmitPolicy transmit,
            string accessToken, AsyncCallback callback)
        {
            AssertSessionNotDeleted();
            if (string.IsNullOrEmpty(accessToken)) throw new ArgumentNullException(nameof(accessToken));
            if (!connectAudio && !connectText)
                throw new ArgumentException($"{GetType().Name}: connectAudio and connectText cannot both be false", nameof(connectAudio));
            if (AudioState != ConnectionState.Disconnected || TextState != ConnectionState.Disconnected)
            {
                throw new InvalidOperationException($"{GetType().Name}: Both AudioState and Text State must be disconnected");
            }

            AsyncNoResult ar = new AsyncNoResult(callback);
            var request = new vx_req_sessiongroup_add_session_t
            {
                account_handle = _loginSession.AccountHandle,
                uri = Key.ToString(),
                session_handle = _sessionHandle,
                sessiongroup_handle = GroupId,
                connect_audio = connectAudio ? 1 : 0,
                connect_text = connectText ? 1 : 0,
                access_token = accessToken
            };

            VxClient.Instance.BeginIssueRequest(request, result =>
            {
                try
                {
                    VxClient.Instance.EndIssueRequest(result);
                    ar.SetComplete();
                }
                catch (Exception e)
                {
                    VivoxDebug.Instance.VxExceptionMessage($"{request.GetType().Name} failed: {e}");
                    AudioState = ConnectionState.Disconnected;
                    TextState = ConnectionState.Disconnected;
                    ar.SetComplete(e);
                    if (VivoxDebug.Instance.throwInternalExcepetions)
                    {
                        throw;
                    }
                    return;
                }
            });

            Debug.WriteLine(transmit);

            if (connectAudio)
            {
                switch (transmit)
                {
                    case TransmitPolicy.No:
                        {
                            if (_loginSession.TransmittingSession == null)
                            {
                                SetNoSessionTransmitting();
                            }
                        }
                        break;
                    case TransmitPolicy.Yes:
                        {
                            SetTransmitting();
                        }
                        break;
                    default:
                        break;
                }
                AudioState = ConnectionState.Connecting;
            }
            if (connectText)
                TextState = ConnectionState.Connecting;

            return ar;
        }

        public void EndConnect(IAsyncResult ar)
        {
            AsyncNoResult parentAr = ar as AsyncNoResult;
            parentAr?.CheckForError();
        }

        public IAsyncResult Disconnect(AsyncCallback callback = null)
        {
            AssertSessionNotDeleted();
            AsyncNoResult ar = new AsyncNoResult(callback);

            if (AudioState == ConnectionState.Connecting || AudioState == ConnectionState.Connected ||
                TextState == ConnectionState.Connecting || TextState == ConnectionState.Connected)
            {
                var request = new vx_req_sessiongroup_remove_session_t
                {
                    session_handle = _sessionHandle,
                    sessiongroup_handle = GroupId
                };
                VxClient.Instance.BeginIssueRequest(request, result =>
                {
                    try
                    {
                        VxClient.Instance.EndIssueRequest(result);
                        _loginSession.ClearTransmittingSession(Channel);
                        ar.SetComplete();
                    }
                    catch (Exception e)
                    {
                        VivoxDebug.Instance.VxExceptionMessage($"{request.GetType().Name} failed: {e}");
                        ar.SetComplete(e);
                        if (VivoxDebug.Instance.throwInternalExcepetions)
                        {
                            throw;
                        }
                        return;
                    }
                    // Don't set the media and text state since that needs to occur from
                    // events or the client will not reflect what's happening on the server.
                });

                if (AudioState != ConnectionState.Disconnected)
                    AudioState = ConnectionState.Disconnecting;
                if (TextState != ConnectionState.Disconnected)
                    TextState = ConnectionState.Disconnecting;
            }
            return ar;
        }

        public IAsyncResult BeginSetAudioConnected(bool value, TransmitPolicy transmit, AsyncCallback callback)
        {
            AssertSessionNotDeleted();

            AsyncNoResult ar = new AsyncNoResult(callback);
            if (value && transmit == TransmitPolicy.Yes)
            {
                SetTransmitting();
            }
            if (AlreadyDone(value, AudioState))
            {
                ar.CompletedSynchronously = true;
                ar.SetComplete();
                return ar;
            }
            if (value)
            {
                var request = new vx_req_session_media_connect_t();
                request.session_handle = _sessionHandle;
                VxClient.Instance.BeginIssueRequest(request, result =>
                {
                    try
                    {
                        VxClient.Instance.EndIssueRequest(result);
                        ar.SetComplete();
                    }
                    catch (Exception e)
                    {
                        VivoxDebug.Instance.VxExceptionMessage($"{request.GetType().Name} failed: {e}");
                        ar.SetComplete(e);
                        if (VivoxDebug.Instance.throwInternalExcepetions)
                        {
                            throw;
                        }
                        return;
                    }
                });
                AudioState = ConnectionState.Connecting;
                return ar;
            }
            else
            {
                _loginSession.ClearTransmittingSession(Channel);

                var request = new vx_req_session_media_disconnect_t();
                request.session_handle = _sessionHandle;
                VxClient.Instance.BeginIssueRequest(request, result =>
                {
                    try
                    {
                        VxClient.Instance.EndIssueRequest(result);
                        ar.SetComplete();
                    }
                    catch (Exception e)
                    {
                        VivoxDebug.Instance.VxExceptionMessage($"{request.GetType().Name} failed: {e}");
                        ar.SetComplete(e);
                        if (VivoxDebug.Instance.throwInternalExcepetions)
                        {
                            throw;
                        }
                        return;
                    }
                });
                AudioState = ConnectionState.Disconnecting;
                return ar;
            }
        }

        public void EndSetAudioConnected(IAsyncResult result)
        {
            AssertSessionNotDeleted();

            AsyncNoResult parentAr = result as AsyncNoResult;
            parentAr?.CheckForError();
        }

        public IAsyncResult BeginSetTextConnected(bool value, AsyncCallback callback)
        {
            AssertSessionNotDeleted();

            AsyncNoResult ar = new AsyncNoResult(callback);
            if (AlreadyDone(value, TextState))
            {
                ar.CompletedSynchronously = true;
                ar.SetComplete();
            }
            if (value)
            {
                var request = new vx_req_session_text_connect_t
                {
                    session_handle = _sessionHandle
                };
                VxClient.Instance.BeginIssueRequest(request, result =>
                {
                    try
                    {
                        VxClient.Instance.EndIssueRequest(result);
                        ar.SetComplete();
                    }
                    catch (Exception e)
                    {
                        VivoxDebug.Instance.VxExceptionMessage($"{request.GetType().Name} failed: {e}");
                        ar.SetComplete(e);
                        if (VivoxDebug.Instance.throwInternalExcepetions)
                        {
                            throw;
                        }
                        return;
                    }
                });
                TextState = ConnectionState.Connecting;
                return ar;
            }
            else
            {
                var request = new vx_req_session_text_disconnect_t();
                request.session_handle = _sessionHandle;
                VxClient.Instance.BeginIssueRequest(request, result =>
                {
                    try
                    {
                        VxClient.Instance.EndIssueRequest(result);
                        ar.SetComplete();
                    }
                    catch (Exception e)
                    {
                        VivoxDebug.Instance.VxExceptionMessage($"{request.GetType().Name} failed: {e}");
                        ar.SetComplete(e);
                        if (VivoxDebug.Instance.throwInternalExcepetions)
                        {
                            throw;
                        }
                        return;
                    }
                });
                TextState = ConnectionState.Disconnecting;
                return ar;
            }
        }

        public void EndSetTextConnected(IAsyncResult result)
        {
            AssertSessionNotDeleted();

            AsyncNoResult parentAr = result as AsyncNoResult;
            parentAr?.CheckForError();
        }

        public ILoginSession Parent => _loginSession;
        public string GroupId { get; }
        public ChannelId Key { get; }

        public ConnectionState AudioState
        {
            get { return _audioState; }
            private set
            {
                if (value != _audioState)
                {
                    _audioState = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(AudioState)));
                }
            }
        }

        public ConnectionState TextState
        {
            get { return _textState; }
            set
            {
                if (value != _textState)
                {
                    _textState = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(TextState)));
                }
            }
        }

        public IReadOnlyDictionary<string, IParticipant> Participants => _participants;

        public bool Typing
        {
            get { return _typing; }

            set
            {
                if (value != _typing)
                {
                    AssertSessionNotDeleted();

                    var request = new vx_req_session_send_notification_t();
                    request.session_handle = _sessionHandle;
                    request.notification_type = (value ? vx_notification_type.notification_typing : vx_notification_type.notification_not_typing);
                    VxClient.Instance.BeginIssueRequest(request, result =>
                    {
                        try
                        {
                            VxClient.Instance.EndIssueRequest(result);
                            _typing = value;
                            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Typing)));
                        }
                        catch (Exception e)
                        {
                            VivoxDebug.Instance.VxExceptionMessage($"{request.GetType().Name} failed: {e}");
                            if (VivoxDebug.Instance.throwInternalExcepetions)
                            {
                                throw;
                            }
                        }
                    });
                }
            }
        }

        public string SessionHandle => _sessionHandle;

        public IAsyncResult BeginSendText(string message, AsyncCallback callback)
        {
            AssertSessionNotDeleted();

            return BeginSendText(null, message, null, null, callback);
        }

        public IAsyncResult BeginSendText(string language, string message, string applicationStanzaNamespace,
            string applicationStanzaBody, AsyncCallback callback)
        {
            AssertSessionNotDeleted();

            if (string.IsNullOrEmpty(message)) throw new ArgumentNullException(nameof(message));
            if (TextState != ConnectionState.Connected)
                throw new InvalidOperationException($"{GetType().Name}: TextState must equal ChannelState.Connected");
            var ar = new AsyncNoResult(callback);
            var request = new vx_req_session_send_message_t
            {
                session_handle = _sessionHandle,
                message_body = message,
                application_stanza_body = applicationStanzaBody,
                language = language,
                application_stanza_namespace = applicationStanzaNamespace
            };
            VxClient.Instance.BeginIssueRequest(request, result =>
            {
                try
                {
                    VxClient.Instance.EndIssueRequest(result);
                    ar.SetComplete();
                }
                catch (VivoxApiException e)
                {
                    VivoxDebug.Instance.VxExceptionMessage($"{request.GetType().Name} failed: {e}");
                    ar.SetComplete(e);
                    if (VivoxDebug.Instance.throwInternalExcepetions)
                    {
                        throw;
                    }
                    return;
                }
                catch (Exception e)
                {
                    VivoxDebug.Instance.VxExceptionMessage($"{request.GetType().Name} failed: {e}");
                    ar.SetComplete(e);
                    if (VivoxDebug.Instance.throwInternalExcepetions)
                    {
                        throw;
                    }
                    return;
                }
            });
            return ar;
        }

        public void EndSendText(IAsyncResult result)
        {
            AssertSessionNotDeleted();
            (result as AsyncNoResult)?.CheckForError();
        }

        public IReadOnlyQueue<IChannelTextMessage> MessageLog => _messageLog;

        public IAsyncResult BeginSessionArchiveQuery(DateTime? timeStart, DateTime? timeEnd, string searchText,
            AccountId userId, uint max, string afterId, string beforeId, int firstMessageIndex,
            AsyncCallback callback)
        {
            AssertSessionNotDeleted();
            if (TextState != ConnectionState.Connected)
                throw new InvalidOperationException($"{GetType().Name}: {nameof(TextState)} must equal ChannelState.Connected");
            if (afterId != null && beforeId != null)
                throw new ArgumentException($"{GetType().Name}: Parameters {nameof(afterId)} and {nameof(beforeId)} cannot be used at the same time");
            if (max > 50)
                throw new ArgumentException($"{GetType().Name}: {nameof(max)} cannot be greater than 50");


            var ar = new AsyncNoResult(callback);

            var request = new vx_req_session_archive_query_t
            {
                session_handle = _sessionHandle,
                max = max,
                after_id = afterId,
                before_id = beforeId,
                first_message_index = firstMessageIndex,
                search_text = searchText
            };
            if (timeStart != null && timeStart != DateTime.MinValue)
            {
                request.time_start = (timeStart?.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ"));
            }
            if (timeEnd != null && timeEnd != DateTime.MaxValue)
            {
                request.time_end = (timeEnd?.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ"));
            }

            if (!AccountId.IsNullOrEmpty(userId))
            {
                request.participant_uri = userId.ToString();
            }

            VxClient.Instance.BeginIssueRequest(request, result =>
            {
                vx_resp_session_archive_query_t response;
                try
                {
                    response = VxClient.Instance.EndIssueRequest(result);
                    _sessionArchiveResult = new ArchiveQueryResult(response.query_id);
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SessionArchiveResult)));
                    ar.SetComplete();
                }
                catch (Exception e)
                {
                    VivoxDebug.Instance.VxExceptionMessage($"{request.GetType().Name} failed: {e}");
                    ar.SetComplete(e);
                    if (VivoxDebug.Instance.throwInternalExcepetions)
                    {
                        throw;
                    }
                    return;
                }
            });
            return ar;
        }

        public void EndSessionArchiveQuery(IAsyncResult result)
        {
            AssertSessionNotDeleted();
            (result as AsyncNoResult)?.CheckForError();
        }

        public IReadOnlyQueue<ISessionArchiveMessage> SessionArchive => _sessionArchive;

        public IArchiveQueryResult SessionArchiveResult => _sessionArchiveResult;

        public string GetConnectToken(string key, TimeSpan expiration)
        {
            AssertSessionNotDeleted();
            return Helper.GetJoinToken(Key.Issuer, expiration, Parent.Key.ToString(), Key.ToString(), key);
        }

        public bool IsTransmittingSession
        {
            get
            {
                return _audioState != ConnectionState.Disconnected && this.Channel.Equals(_loginSession.TransmittingSession);
            }
            set
            {
                if (value == false)
                {
                    if (!Channel.Equals(_loginSession.TransmittingSession))
                        return;
                    SetNoSessionTransmitting();
                }
                else
                {
                    if (_audioState != ConnectionState.Connected)
                        throw new InvalidOperationException(
                            $"{GetType().Name}: {0} must be connected to audio before setting to transmitting");
                    SetTransmitting();
                }
            }
        }

        private void SetTransmitting()
        {
            var request = new vx_req_sessiongroup_set_tx_session_t();
            request.session_handle = this._sessionHandle;
            _loginSession.TransmittingSession = Channel;
            VxClient.Instance.BeginIssueRequest(request, result =>
            {
                try
                {
                    VxClient.Instance.EndIssueRequest(result);
                }
                catch (Exception e)
                {
                    VivoxDebug.Instance.VxExceptionMessage($"{request.GetType().Name} failed: {e}");
                    if (VivoxDebug.Instance.throwInternalExcepetions)
                    {
                        throw;
                    }
                }
            });
        }

        private void SetNoSessionTransmitting()
        {
            var request = new vx_req_sessiongroup_set_tx_no_session_t();
            request.sessiongroup_handle = GroupId;
            _loginSession.TransmittingSession = null;
            VxClient.Instance.BeginIssueRequest(request, result =>
            {
                try
                {
                    VxClient.Instance.EndIssueRequest(result);
                }
                catch (Exception e)
                {
                    VivoxDebug.Instance.VxExceptionMessage($"{request.GetType().Name} failed: {e}");
                    if (VivoxDebug.Instance.throwInternalExcepetions)
                    {
                        throw;
                    }
                }
            });
        }

        public ChannelId Channel => Key;

        public void Set3DPosition(UnityEngine.Vector3 speakerPos, UnityEngine.Vector3 listenerPos, UnityEngine.Vector3 listenerAtOrient, UnityEngine.Vector3 listenerUpOrient)
        {
            if (Channel.Type != ChannelType.Positional)
            {
                throw new InvalidOperationException($"{GetType().Name}: Set3DPosition() failed for InvalidState: Channel must be Positional.");
            }

            if (!((AudioState == ConnectionState.Connected && (TextState == ConnectionState.Connected || TextState == ConnectionState.Disconnected)) || (TextState == ConnectionState.Connected && AudioState == ConnectionState.Disconnected)))
            {
                throw new InvalidOperationException($"{GetType().Name}: Set3DPosition() failed for InvalidState: The channel's AudioState must be connected");
            }
            var request = new vx_req_session_set_3d_position_t();
            request.session_handle = _sessionHandle;
            request.Set3DPosition(
            new float[3] { speakerPos.x, speakerPos.y, speakerPos.z },
            new float[3] { listenerPos.x, listenerPos.y, listenerPos.z },
            new float[3] { listenerAtOrient.x, listenerAtOrient.y, listenerAtOrient.z },
            new float[3] { listenerUpOrient.x, listenerUpOrient.y, listenerUpOrient.z }
            );
            VxClient.Instance.BeginIssueRequest(request, result =>
            {
                try
                {
                    VxClient.Instance.EndIssueRequest(result);
                }
                catch (Exception e)
                {
                    VivoxDebug.Instance.VxExceptionMessage($"{request.GetType().Name} failed: {e}");
                    if (VivoxDebug.Instance.throwInternalExcepetions)
                    {
                        throw;
                    }
                }
            });
        }

        #endregion

        public void Delete()
        {
            Disconnect();
            _deleted = true;
        }
    }
}
