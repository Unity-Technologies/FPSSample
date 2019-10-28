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
using System.Linq;

namespace VivoxUnity.Private
{
    internal class LoginSession : ILoginSession
    {
        #region Member Variables

        private readonly string _accountHandle;

        private readonly string _groupHandle;
        private LoginState _state = LoginState.LoggedOut;
        private bool _isInjectingAudio = false;
        private readonly ReadWriteDictionary<ChannelId, IChannelSession, ChannelSession> _channelSessions = new ReadWriteDictionary<ChannelId, IChannelSession, ChannelSession>();
        private readonly ReadWriteHashSet<AccountId> _blockedSubscriptions = new ReadWriteHashSet<AccountId>();
        private readonly ReadWriteHashSet<AccountId> _allowedSubscriptions = new ReadWriteHashSet<AccountId>();
        private readonly ReadWriteDictionary<AccountId, IPresenceSubscription, PresenceSubscription> _presenceSubscriptions = new ReadWriteDictionary<AccountId, IPresenceSubscription, PresenceSubscription>();
        private Presence _presence;
        private readonly Client _client;
        private readonly ReadWriteQueue<IDirectedTextMessage> _directedMessages = new ReadWriteQueue<IDirectedTextMessage>();
        private readonly ReadWriteQueue<IFailedDirectedTextMessage> _failedDirectedMessages = new ReadWriteQueue<IFailedDirectedTextMessage>();
        private readonly ReadWriteQueue<IAccountArchiveMessage> _accountArchive = new ReadWriteQueue<IAccountArchiveMessage>();
        //TODO: Currently there needs to be 2 seconds between the last message send and the account archive query, this should be fixed
        private DateTime lastMessageTime;
        private DirectedMessageResult _directedMessageResult = new DirectedMessageResult();
        private ArchiveQueryResult _accountArchiveResult = new ArchiveQueryResult();
        //
        private readonly ReadWriteQueue<AccountId> _incomingSubscriptionRequests = new ReadWriteQueue<AccountId>();
        private ChannelId _transmittingSession;
        private ParticipantPropertyUpdateFrequency _participantPropertyFrequency = ParticipantPropertyUpdateFrequency.StateChange;
        #endregion

        #region Events
        public event PropertyChangedEventHandler PropertyChanged;
        #endregion

        #region Helpers

        void AssertLoggedIn()
        {
            if (_state != LoginState.LoggedIn)
                throw new InvalidOperationException($"{GetType().Name}: Invalid State - must be logged in to perform this operation.");
        }
        void AssertLoggedOut()
        {
            if (_state != LoginState.LoggedOut)
                throw new InvalidOperationException($"{GetType().Name}: Invalid State - must be logged out to perform this operation.");
        }

        #endregion

        internal LoginSession(Client client, AccountId accountId)
        {
            if (AccountId.IsNullOrEmpty(accountId)) throw new ArgumentNullException(nameof(accountId));
            if (client == null) throw new ArgumentNullException(nameof(client));
            Key = accountId;
            _accountHandle = accountId.ToString();
            _groupHandle = "sg_" + _accountHandle;
            _client = client;
            VxClient.Instance.EventMessageReceived += Instance_EventMessageReceived;
        }

        private void Instance_EventMessageReceived(vx_evt_base_t eventMessage)
        {
            switch ((vx_event_type)eventMessage.type)
            {
                case vx_event_type.evt_account_login_state_change:
                    HandleAccountLoginStateChangeEvt(eventMessage);
                    break;
                case vx_event_type.evt_buddy_presence:
                    HandleBuddyPresenceEvt(eventMessage);
                    break;
                case vx_event_type.evt_user_to_user_message:
                    HandleUserToUserMessage(eventMessage);
                    break;
                case vx_event_type.evt_subscription:
                    HandleSubscription(eventMessage);
                    break;
                case vx_event_type.evt_account_archive_message:
                   HandleAccountArchiveMessage(eventMessage);
                   break;
                case vx_event_type.evt_account_archive_query_end:
                   HandleAccountArchiveQueryEnd(eventMessage);
                   break;
                case vx_event_type.evt_media_completion:
                    HandleMediaComplete(eventMessage);
                    break;
                case vx_event_type.evt_account_send_message_failed:
                    HandleAccountSendMessageFailed(eventMessage);
                    break;
            }
        }

        public void HandleMediaComplete(vx_evt_base_t eventMessage)
        {
            vx_evt_media_completion_t evt = eventMessage;
            Debug.Assert(evt != null);
            switch (evt.completion_type)
            {
                case vx_media_completion_type.sessiongroup_audio_injection:
                    IsInjectingAudio = false;
                    break;
            }
        }
        private void HandleBuddyPresenceEvt(vx_evt_base_t eventMessage)
        {
            vx_evt_buddy_presence_t evt = eventMessage;
            if (evt.account_handle != _accountHandle) return;

            var buddyAccount = new AccountId(evt.buddy_uri, evt.displayname);
            if (!_presenceSubscriptions.ContainsKey(buddyAccount)) return;

            var subscription = (PresenceSubscription)_presenceSubscriptions[buddyAccount];
            subscription.UpdateLocation(evt.buddy_uri, (PresenceStatus)evt.presence,
                                        evt.custom_message);
        }

        private void HandleUserToUserMessage(vx_evt_base_t eventMessage)
        {
            vx_evt_user_to_user_message_t evt = eventMessage;
            if (evt.account_handle != _accountHandle) return;
            Debug.Assert(evt != null);
            _directedMessages.Enqueue(new DirectedTextMessage
            {
                ReceivedTime = DateTime.Now,
                Message = evt.message_body,
                Sender = new AccountId(evt.from_uri),
                ApplicationStanzaBody = evt.application_stanza_body,
                ApplicationStanzaNamespace = evt.application_stanza_namespace,
                Language = evt.language, 
                LoginSession = this
            });
        }

        private void HandleAccountLoginStateChangeEvt(vx_evt_base_t eventMessage) 
       {
            vx_evt_account_login_state_change_t evt = eventMessage; 
            Debug.Assert(evt != null); 
            if (evt.account_handle != _accountHandle) return;
            // Using the property, since its setter includes triggering the PropertyChanged event
            
            
            State = (LoginState) evt.state; 
       }

        private void HandleSubscription(vx_evt_base_t eventMessage)
        {
            vx_evt_subscription_t evt = eventMessage;
            Debug.Assert(evt != null);
            if (evt.account_handle != _accountHandle) return;
            _incomingSubscriptionRequests.Enqueue(new AccountId(evt.buddy_uri));
        }

        private void HandleAccountArchiveMessage(vx_evt_base_t eventMessage)
        {
            vx_evt_account_archive_message_t evt = eventMessage;
            if (evt.account_handle != _accountHandle) return;

            DateTime parsedReceivedTime;

            if(!DateTime.TryParse(evt.time_stamp, out parsedReceivedTime))
            {
                VivoxDebug.Instance.DebugMessage($"{GetType().Name}: {eventMessage.GetType().Name} invalid message: Bad time format", vx_log_level.log_error);
                Debug.Assert(false);
                return;
            }

            var message = new AccountArchiveMessage()
            {
                LoginSession = this,
                Key = evt.message_id,
                MessageId = evt.message_id,
                QueryId = evt.query_id,
                ReceivedTime = parsedReceivedTime,
                Message = evt.message_body,
                Inbound = (evt.is_inbound != 0),
                Language = evt.language,
                RemoteParticipant = new AccountId(evt.participant_uri),
                Channel = new ChannelId(evt.channel_uri)
            };
            _accountArchive.Enqueue(message);
        }

        private void HandleAccountArchiveQueryEnd(vx_evt_base_t eventMessage)
        {
            vx_evt_account_archive_query_end_t evt = eventMessage;
            if (evt.account_handle != _accountHandle) return;
            
            if (_accountArchiveResult.QueryId != evt.query_id || !_accountArchiveResult.Running) return;
            _accountArchiveResult = new ArchiveQueryResult(evt);
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(AccountArchiveResult)));
        }

        private void HandleAccountSendMessageFailed(vx_evt_base_t eventMessage)
        {
            vx_evt_account_send_message_failed_t evt = eventMessage;
            Debug.Assert(evt != null);
            AssertLoggedIn();
            if (evt.account_handle != _accountHandle) return;
            if (_directedMessageResult.RequestId != evt.request_id) return;
            _failedDirectedMessages.Enqueue(new FailedDirectedTextMessage
            {
                Sender = new AccountId(evt.account_handle),
                RequestId = evt.request_id,
                StatusCode = evt.status_code
            });
        }

internal string AccountHandle => _accountHandle;

        #region ILoginSession
        public IAsyncResult BeginLogin(
            Uri server,
            string accessToken,
            SubscriptionMode subscriptionMode,
            IReadOnlyHashSet<AccountId> presenceSubscriptions,
            IReadOnlyHashSet<AccountId> blockedPresenceSubscriptions,
            IReadOnlyHashSet<AccountId> allowedPresenceSubscriptions,
            AsyncCallback callback)
        {
            if (string.IsNullOrEmpty(accessToken)) throw new ArgumentNullException(nameof(accessToken));

            AssertLoggedOut();
            AsyncNoResult result = new AsyncNoResult(callback);
            State = LoginState.LoggingIn;

            _client.BeginGetConnectorHandle(server, ar2 =>
            {
                string connectorHandle;
                try
                {
                    connectorHandle = _client.EndGetConnectorHandle(ar2);
                }
                catch (Exception e)
                {
                    VivoxDebug.Instance.VxExceptionMessage($"BeginGetConnectorHandle failed: {e}");
                    State = LoginState.LoggedOut;
                    result.SetComplete(e);
                    if (VivoxDebug.Instance.throwInternalExcepetions)
                    {
                        throw;
                    }
                    return;
                }
                Debug.Write($"connectorHandle={connectorHandle}");
                Login(accessToken, connectorHandle, result, subscriptionMode);
            });
            return result;
        }

        public IAsyncResult BeginLogin(Uri server,
                        string accessToken,
                        AsyncCallback callback)
        {
            if (string.IsNullOrEmpty(accessToken)) throw new ArgumentNullException(nameof(accessToken));

            AssertLoggedOut();
            AsyncNoResult result = new AsyncNoResult(callback);

            State = LoginState.LoggingIn;
            _client.BeginGetConnectorHandle(server, ar2 =>
            {
                string connectorHandle;
                try
                {
                    connectorHandle = _client.EndGetConnectorHandle(ar2);

                    VivoxDebug.Instance.DebugMessage($"{GetType().Name}: connectorHandle={connectorHandle}", vx_log_level.log_debug);
                    Login(accessToken, connectorHandle, result);
                }
                catch (Exception e)
                {
                    VivoxDebug.Instance.VxExceptionMessage($"BeginGetConnectorHandle failed: {e}");
                    State = LoginState.LoggedOut;
                    result.SetComplete(e);
                    if (VivoxDebug.Instance.throwInternalExcepetions)
                    {
                        throw;
                    }
                    return;
                }
                
            });
            return result;
        }

        private void Login(string accessToken, string connectorHandle, AsyncNoResult ar, SubscriptionMode? mode = null)
        {
            vx_req_account_anonymous_login_t request = new vx_req_account_anonymous_login_t
            {
                account_handle = _accountHandle,
                connector_handle = connectorHandle,
                enable_buddies_and_presence = mode == null ? 0 : 1,
                acct_name = Key.AccountName,
                displayname = Key.DisplayName,
                access_token = accessToken,
                participant_property_frequency = (int)_participantPropertyFrequency
            };

            if (mode != null)
            {
                request.buddy_management_mode = (vx_buddy_management_mode)mode.Value;
            }
            VxClient.Instance.BeginIssueRequest(request, result =>
            {
                try
                {
                    VxClient.Instance.EndIssueRequest(result);
                    State = LoginState.LoggedIn;
                    ar.SetComplete();
                }
                catch (Exception e)
                {
                    VivoxDebug.Instance.VxExceptionMessage($"{request.GetType().Name} failed: {e}");
                    State = LoginState.LoggedOut;
                    ar.SetComplete(e);
                    if (VivoxDebug.Instance.throwInternalExcepetions)
                    {
                        throw;
                    }
                    return;
                }
            });
        }

        public void EndLogin(IAsyncResult result)
        {
            AsyncNoResult ar = result as AsyncNoResult;
            ar?.CheckForError();
        }
        
        public IAsyncResult BeginAccountSetLoginProperties(ParticipantPropertyUpdateFrequency participantPropertyFrequency, AsyncCallback callback)
        {
            AssertLoggedIn();
            AsyncNoResult ar = new AsyncNoResult(callback);

            var request = new vx_req_account_set_login_properties_t
            {
                account_handle = _accountHandle,
                participant_property_frequency = (int)participantPropertyFrequency
            };
            VxClient.Instance.BeginIssueRequest(request, result =>
            {
                try
                {
                    VxClient.Instance.EndIssueRequest(result);
                    _participantPropertyFrequency = participantPropertyFrequency;
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

        public void EndAccountSetLoginProperties(IAsyncResult result)
        {
            AssertLoggedIn();
            AsyncNoResult ar = result as AsyncNoResult;
            ar?.CheckForError();
        }

        public AccountId Key { get; }

        public LoginState State
        {
            get { return _state; }
            private set
            {
                if (_state != value)
                {
                    _state = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(State)));
                }
            }
        }

        public bool IsInjectingAudio
        {
            get { return _isInjectingAudio; }
            private set
            {
                if (_isInjectingAudio != value)
                {
                    _isInjectingAudio = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsInjectingAudio)));
                }
            }
        }

        public IReadOnlyDictionary<ChannelId, IChannelSession> ChannelSessions => _channelSessions;

        public IChannelSession GetChannelSession(ChannelId channelId)
        {
            if (ChannelId.IsNullOrEmpty(channelId)) throw new ArgumentNullException(nameof(channelId));
            AssertLoggedIn();
            if (_channelSessions.ContainsKey(channelId))
            {
                return _channelSessions[channelId];
            }
            var c = new ChannelSession(this, channelId, _groupHandle);
            _channelSessions[channelId] = c;
            return c;
        }

        public IReadOnlyDictionary<AccountId, IPresenceSubscription> PresenceSubscriptions => _presenceSubscriptions;
        public IReadOnlyHashSet<AccountId> BlockedSubscriptions => _blockedSubscriptions;

        public IAsyncResult BeginAddBlockedSubscription(AccountId userId, AsyncCallback callback)
        {
            AssertLoggedIn();
            AsyncNoResult ar = new AsyncNoResult(callback);
            if (_blockedSubscriptions.Contains((userId)))
            {
                ar.SetCompletedSynchronously();
                return ar;
            }
            var request = new vx_req_account_create_block_rule_t
            {
                account_handle = _accountHandle,
                block_mask = userId.ToString(),
                presence_only = 0
            };

            VxClient.Instance.BeginIssueRequest(request, result =>
            {
                try
                {
                    VxClient.Instance.EndIssueRequest(result);
                    _blockedSubscriptions.Add(userId);
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

        public void EndAddBlockedSubscription(IAsyncResult result)
        {
            AssertLoggedIn();
            (result as AsyncNoResult)?.CheckForError();
        }

        public IAsyncResult BeginRemoveBlockedSubscription(AccountId userId, AsyncCallback callback)
        {
            AssertLoggedIn();
            AsyncNoResult ar = new AsyncNoResult(callback);
            if (!_blockedSubscriptions.Contains((userId)))
            {
                ar.SetCompletedSynchronously();
                return ar;
            }
            var request = new vx_req_account_delete_block_rule_t
            {
                account_handle = _accountHandle,
                block_mask = userId.ToString()
            };

            VxClient.Instance.BeginIssueRequest(request, result =>
            {
                try
                {
                    VxClient.Instance.EndIssueRequest(result);
                    _blockedSubscriptions.Remove(userId);
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

        public void EndRemoveBlockedSubscription(IAsyncResult result)
        {
            AssertLoggedIn();
            (result as AsyncNoResult)?.CheckForError();
        }

        public IReadOnlyHashSet<AccountId> AllowedSubscriptions => _allowedSubscriptions;

        public Presence Presence
        {
            get { return _presence; }
            set
            {
                AssertLoggedIn();
                if (!Equals(_presence, value))
                {
                    AsyncNoResult ar = new AsyncNoResult(null);

                    var request = new vx_req_account_set_presence_t
                    {
                        account_handle = _accountHandle,
                        custom_message = value.Message,
                        presence = (vx_buddy_presence_state)value.Status
                    };

                    VxClient.Instance.BeginIssueRequest(request, result =>
                    {
                        try
                        {
                            VxClient.Instance.EndIssueRequest(result);
                            _presence = value;
                            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Presence)));
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
                        }
                    });
                }
            }
        }

        public IAsyncResult BeginSendDirectedMessage(AccountId userId, string message, AsyncCallback callback)
        {
            return BeginSendDirectedMessage(userId, null, message, null, null, callback);
        }

        public IAsyncResult BeginSendDirectedMessage(AccountId userId, string language, string message, string applicationStanzaNamespace, string applicationStanzaBody, AsyncCallback callback)
        {
            if (AccountId.IsNullOrEmpty(userId)) throw new ArgumentNullException(nameof(userId));
            if (string.IsNullOrEmpty(message) && string.IsNullOrEmpty(applicationStanzaBody)) throw new ArgumentNullException($"{nameof(message)} and {nameof(applicationStanzaBody)} cannot both be null");

            AssertLoggedIn();
            AsyncNoResult ar = new AsyncNoResult(callback);

            var request = new vx_req_account_send_message_t
            {
                account_handle = _accountHandle,
                message_body = message,
                user_uri = userId.ToString(),
                language = language,
                application_stanza_namespace = applicationStanzaNamespace,
                application_stanza_body = applicationStanzaBody
            };

            VxClient.Instance.BeginIssueRequest(request, result =>
            {
                vx_resp_account_send_message_t response;
                try
                {
                    response = VxClient.Instance.EndIssueRequest(result);
                    _directedMessageResult = new DirectedMessageResult(response.request_id);
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(DirectedMessageResult)));
                    ar.SetComplete();
                }
                catch (Exception e)
                {
                    VivoxDebug.Instance.VxExceptionMessage($"{request.GetType().Name} failed: {e}");
                    ar.SetComplete(e);
                    return;
                }
            });
            return ar;
        }

        public void EndSendDirectedMessage(IAsyncResult result)
        {
            AssertLoggedIn();
            Console.WriteLine("Finishing message: " + DateTime.UtcNow);
            lastMessageTime = DateTime.UtcNow;
            Console.WriteLine(lastMessageTime.ToString());
            (result as AsyncNoResult)?.CheckForError();
        }

        public IAsyncResult BeginAccountArchiveQuery(DateTime? timeStart, DateTime? timeEnd, string searchText,
           AccountId userId, ChannelId channel, uint max, string afterId, string beforeId, int firstMessageIndex,
           AsyncCallback callback)
        {
            AssertLoggedIn();
            if (userId != null && channel != null)
                throw new ArgumentException($"{GetType().Name}: Parameters {nameof(userId)} and {nameof(channel)} cannot be used at the same time");
            if (afterId != null && beforeId != null)
                throw new ArgumentException($"{GetType().Name}: Parameters {nameof(afterId)} and {nameof(beforeId)} cannot be used at the same time");
            if (max > 50)
                throw new ArgumentException($"{GetType().Name}: {nameof(max)} cannot be greater than 50");

            var ar = new AsyncNoResult(callback);

            var request = new vx_req_account_archive_query_t
            {
                account_handle = _accountHandle,
                max = max,
                after_id = afterId,
                before_id = beforeId,
                first_message_index = firstMessageIndex
            };

            if (timeStart != null && timeStart != DateTime.MinValue)
            {
                request.time_start = timeStart?.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ");
            }
            if (timeEnd != null && timeEnd != DateTime.MaxValue)
            {
                request.time_end = timeEnd?.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ");
            }
            request.search_text = searchText;
            if (userId != null)
            {
                request.participant_uri = userId.ToString();
            }
            else if (channel != null)
            {
                request.participant_uri = channel.ToString();
            }

            VxClient.Instance.BeginIssueRequest(request, result =>
            {
                vx_resp_account_archive_query_t response;
                try
                {
                    response = VxClient.Instance.EndIssueRequest(result);
                    _accountArchiveResult = new ArchiveQueryResult(response.query_id);
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(AccountArchiveResult)));
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
        public void EndAccountArchiveQuery(IAsyncResult result)
        {
            AssertLoggedIn();
            (result as AsyncNoResult)?.CheckForError();
        }

        public void Logout()
        {
            if (_state == LoginState.LoggedIn || _state == LoginState.LoggingIn)
            {
                var request = new vx_req_account_logout_t();
                request.account_handle = _accountHandle;
                VxClient.Instance.BeginIssueRequest(request, null);
                _presenceSubscriptions.Clear();
                _allowedSubscriptions.Clear();
                _blockedSubscriptions.Clear();
                _channelSessions.Clear();
                _directedMessages.Clear();
                _failedDirectedMessages.Clear();
                _incomingSubscriptionRequests.Clear();
                _transmittingSession = null;
                VxClient.Instance.EventMessageReceived -= Instance_EventMessageReceived;
                // Specifically do not change the property in a way that raises an event
                _state = LoginState.LoggedOut;
            }
        }

        public IAsyncResult BeginAddAllowedSubscription(AccountId userId, AsyncCallback callback)
        {
            AssertLoggedIn();
            AsyncNoResult ar = new AsyncNoResult(callback);

            if (_allowedSubscriptions.Contains((userId)))
            {
                ar.SetCompletedSynchronously();
                return ar;
            }
            if (_incomingSubscriptionRequests.Contains(userId))
            {
                var request = new vx_req_account_send_subscription_reply_t();
                request.account_handle = _accountHandle;
                request.buddy_uri = userId.ToString();
                request.rule_type = vx_rule_type.rule_allow;
                VxClient.Instance.BeginIssueRequest(request, result =>
                {
                    try
                    {
                        VxClient.Instance.EndIssueRequest(result);
                        _incomingSubscriptionRequests.RemoveAll(userId);
                        ar.SetComplete();
                    }
                    catch (Exception e)
                    {
                        VivoxDebug.Instance.VxExceptionMessage($"{request.GetType().Name} failed: {e}");
                        ar.SetComplete(e);
                        return;
                    }
                });
                return ar;
            }
            else
            {
                var request = new vx_req_account_create_auto_accept_rule_t
                {
                    account_handle = _accountHandle,
                    auto_accept_mask = userId.ToString()
                };
                VxClient.Instance.BeginIssueRequest(request, result =>
                {
                    try
                    {
                        VxClient.Instance.EndIssueRequest(result);
                        _allowedSubscriptions.Add(userId);
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
        }

        public void EndAddAllowedSubscription(IAsyncResult result)
        {
            AssertLoggedIn();
            (result as AsyncNoResult)?.CheckForError();
        }

        public IAsyncResult BeginRemoveAllowedSubscription(AccountId userId, AsyncCallback callback)
        {
            AssertLoggedIn();
            AsyncNoResult ar = new AsyncNoResult(callback);

            if (!_allowedSubscriptions.Contains((userId)))
            {
                ar.SetCompletedSynchronously();
                return ar;
            }
            var request = new vx_req_account_delete_auto_accept_rule_t
            {
                account_handle = _accountHandle,
                auto_accept_mask = userId.ToString()
            };
            VxClient.Instance.BeginIssueRequest(request, result =>
            {
                try
                {
                    VxClient.Instance.EndIssueRequest(result);
                    _allowedSubscriptions.Remove(userId);
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

        public void EndRemoveAllowedSubscription(IAsyncResult result)
        {
            AssertLoggedIn();
            (result as AsyncNoResult)?.CheckForError();
        }

        public IAsyncResult BeginAddPresenceSubscription(AccountId userId, AsyncCallback callback)
        {
            AssertLoggedIn();
            var ar = new AsyncResult<IPresenceSubscription>(callback);

            if (_presenceSubscriptions.ContainsKey((userId)))
            {
                ar.SetCompletedSynchronously(_presenceSubscriptions[userId]);
                return ar;
            }
            var request = new vx_req_account_buddy_set_t
            {
                account_handle = _accountHandle,
                buddy_uri = userId.ToString()
            };
            VxClient.Instance.BeginIssueRequest(request, result =>
            {
                try
                {
                    VxClient.Instance.EndIssueRequest(result);
                    _presenceSubscriptions[userId] = new PresenceSubscription
                    {
                        Key = userId
                    };
                    ar.SetComplete(_presenceSubscriptions[userId]);
                    BeginRemoveBlockedSubscription(userId, ar2 =>
                    {
                        try
                        {
                            EndRemoveBlockedSubscription(ar2);
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
                        BeginAddAllowedSubscription(userId, ar3 =>
                        {
                            try
                            {
                                EndAddAllowedSubscription(ar3);
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
                    });
                }
                catch (Exception e)
                {
                    VivoxDebug.Instance.DebugMessage($"{GetType().Name}: {request.GetType().Name} failed {e}", vx_log_level.log_error);
                    ar.SetComplete(e);
                    return;
                }
            });
            return ar;
        }

        public IPresenceSubscription EndAddPresenceSubscription(IAsyncResult result)
        {
            AssertLoggedIn();
            return (result as AsyncResult<IPresenceSubscription>)?.Result;
        }

        public IAsyncResult BeginRemovePresenceSubscription(AccountId userId, AsyncCallback callback)
        {
            AssertLoggedIn();
            AsyncNoResult ar = new AsyncNoResult(callback);

            if (!_presenceSubscriptions.ContainsKey((userId)))
            {
                ar.SetCompletedSynchronously();
                return ar;
            }
            var request = new vx_req_account_buddy_delete_t();
            request.account_handle = _accountHandle;
            request.buddy_uri = userId.ToString();
            VxClient.Instance.BeginIssueRequest(request, result =>
            {
                try
                {
                    VxClient.Instance.EndIssueRequest(result);
                    _presenceSubscriptions.Remove(userId);
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

        public void EndRemovePresenceSubscription(IAsyncResult result)
        {
            AssertLoggedIn();
            (result as AsyncNoResult)?.CheckForError();
        }

        public string GetLoginToken(string key, TimeSpan expiration)
        {
            return VivoxCoreInstance.vx_debug_generate_token(Key.Issuer, (int)expiration.TotalSeconds, "login", Helper.serialNumber++, null, this.Key.ToString(), null, key);
        }

        public IReadOnlyQueue<IDirectedTextMessage> DirectedMessages => _directedMessages;
        public IReadOnlyQueue<IFailedDirectedTextMessage> FailedDirectedMessages => _failedDirectedMessages;

        public IReadOnlyQueue<IAccountArchiveMessage> AccountArchive => _accountArchive;
        public IArchiveQueryResult AccountArchiveResult => _accountArchiveResult;
        public IDirectedMessageResult DirectedMessageResult => _directedMessageResult;

        public IReadOnlyQueue<AccountId> IncomingSubscriptionRequests => _incomingSubscriptionRequests;
        public AccountId LoginSessionId => Key;
        public ParticipantPropertyUpdateFrequency ParticipantPropertyFrequency
        {
            get { return _participantPropertyFrequency; }
            set
            {
                AssertLoggedOut();
                _participantPropertyFrequency = value;
            }
        }

        public void DeleteChannelSession(ChannelId channelId)
        {
            if (_channelSessions.ContainsKey(channelId))
            {
                (_channelSessions[channelId] as ChannelSession)?.Delete();
                _channelSessions.Remove(channelId);
            }
        }

        public ChannelId TransmittingSession
        {
            get { return _transmittingSession; }
            set
            {
                if (value == null && _transmittingSession == null)
                    return;
                if ((value == null || _transmittingSession == null) || !value.Equals(_transmittingSession))
                {
                    _transmittingSession = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(TransmittingSession)));
                }
            }
        }
          public void StartAudioInjection(string audioFilePath)
        {
            if (!ChannelSessions.Any((cs) => cs.AudioState == ConnectionState.Connected))
            {
                throw new InvalidOperationException($"{GetType().Name}: StartAudioInjection() failed for InvalidState: The channel's AudioState must be connected");
            }
            vx_req_sessiongroup_control_audio_injection_t request;

            request = new vx_req_sessiongroup_control_audio_injection_t
            {
                sessiongroup_handle = _groupHandle,
                filename = audioFilePath,
                audio_injection_control_type = vx_sessiongroup_audio_injection_control_type.vx_sessiongroup_audio_injection_control_start
            };

            VxClient.Instance.BeginIssueRequest(request, result =>
            {
                try
                {
                    VxClient.Instance.EndIssueRequest(result);
                    IsInjectingAudio = true;
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

        public void StopAudioInjection()
        {
            if (!ChannelSessions.Any((cs) => cs.AudioState == ConnectionState.Connected || cs.TextState == ConnectionState.Connected) 
                || IsInjectingAudio == false)
            {
                VivoxDebug.Instance.DebugMessage($"{GetType().Name}: StopAudioInjection() warning; No audio injection to stop", vx_log_level.log_warning);
                return;
            }
            vx_req_sessiongroup_control_audio_injection_t request;

            request = new vx_req_sessiongroup_control_audio_injection_t
            {
                sessiongroup_handle = _groupHandle,
                audio_injection_control_type = vx_sessiongroup_audio_injection_control_type.vx_sessiongroup_audio_injection_control_stop
            };

            VxClient.Instance.BeginIssueRequest(request, result =>
            {
                try
                {
                    VxClient.Instance.EndIssueRequest(result);
                    IsInjectingAudio = false;
                }
                catch (Exception e)
                {
                    VivoxDebug.Instance.VxExceptionMessage($"{request.GetType().Name} failed: {e}");
                }
            });
        }
        #endregion

        internal void ClearTransmittingSession(ChannelId channelId)
        {
            if (_transmittingSession == null)
                return;
            if (_transmittingSession.Equals(channelId))
                _transmittingSession = null;
        }
    }
}
