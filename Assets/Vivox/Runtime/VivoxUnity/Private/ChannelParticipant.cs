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

namespace VivoxUnity.Private
{
    internal class ChannelParticipant : IParticipant
    {
        #region Member Variables

        private readonly ChannelSession _parent;
        private bool _speechDetected;
        private bool _isTyping;
        private bool _textActive;
        private bool _audioActive;
        private double _audioEnergy;
        private bool _isMutedForEveryone;

        // A layer of abstraction to allow events to set the value when fired from core
        private bool _localMute;
        internal bool _internalMute
        {
            get { return _localMute; }
            set
            {
                _localMute = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(LocalMute)));
            }
        }

        // A layer of abstraction to allow events to set the value when fired from core
        private int _localVolumeAdjustment;
        internal int _internalVolumeAdjustment
        {
            get { return _localVolumeAdjustment; }
            set
            {
                _localVolumeAdjustment = value - 50;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(LocalVolumeAdjustment)));
            }
        }

        #endregion
        public ChannelParticipant(ChannelSession parent, vx_evt_participant_added_t theEvent)
        {
            _parent = parent;
            IsSelf = theEvent.is_current_user != 0;
            Key = theEvent.participant_uri;
            Account = new AccountId(theEvent.participant_uri, theEvent.displayname);
        }
        #region IParticipant
        public event PropertyChangedEventHandler PropertyChanged;
        public IChannelSession ParentChannelSession => _parent;
        public bool IsSelf { get; }

        public IAsyncResult SetIsMuteForAll(string accountHandle, bool setMuted, string accessToken, AsyncCallback callback)
        {
            if (string.IsNullOrEmpty(accessToken)) throw new ArgumentNullException(nameof(accessToken));

            AsyncNoResult ar = new AsyncNoResult(callback);
            var request = new vx_req_channel_mute_user_t
            {
                account_handle = accountHandle,
                channel_uri = _parent.Key.ToString(),
                participant_uri = Account.ToString(),
                set_muted = setMuted ? 1 : 0,
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

        public AccountId Account { get; }

        public string Key { get; }

        public bool SpeechDetected
        {
            get
            {
                return _speechDetected;
            }
            set
            {
                if (_speechDetected != value)
                {
                    _speechDetected = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SpeechDetected)));
                }
            }
        }

        public bool IsMutedForAll
        {
            get { return _isMutedForEveryone; }
            set
            {
                if (_isMutedForEveryone != value)
                {
                    _isMutedForEveryone = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsMutedForAll)));
                }

            }
        }

        public bool LocalMute
        {
            get { return _localMute; }
            set
            {
                if (_internalMute != value)
                {
                    var request = new vx_req_session_set_participant_mute_for_me_t
                    {
                        mute = value ? 1 : 0,
                        participant_uri = Account.ToString(),
                        session_handle = _parent.SessionHandle
                    };
                    VxClient.Instance.BeginIssueRequest(request.base_, result =>
                    {
                        try
                        {
                            VxClient.Instance.EndIssueRequest(result);
                            _internalMute = value;
                            //Propchange fires in local variable setter
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

        public int LocalVolumeAdjustment
        {
            get { return _localVolumeAdjustment; }
            set
            {
                if (value < -50 || value > 50)
                    throw new ArgumentOutOfRangeException(nameof(LocalVolumeAdjustment));
                if (_internalVolumeAdjustment != value)
                {
                    var request = new vx_req_session_set_participant_volume_for_me_t
                    {
                        volume = (value + 50),
                        participant_uri = (Account.ToString()),
                        session_handle = (_parent.SessionHandle)
                    };
                    VxClient.Instance.BeginIssueRequest(request.base_, result =>
                    {
                        try
                        {
                            VxClient.Instance.EndIssueRequest(result);
                            _internalVolumeAdjustment = request.volume;
                            //Propchange fires in local variable setter
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

        public bool IsTyping
        {
            get
            {
                return _isTyping;
            }
            set
            {
                if (_isTyping != value)
                {
                    _isTyping = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsTyping)));
                }
            }
        }

        public bool InText
        {
            get { return _textActive; }
            set
            {
                if (_textActive != value)
                {
                    _textActive = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(InText)));
                }
            }
        }
        public bool InAudio
        {
            get { return _audioActive; }
            set
            {
                if (_audioActive != value)
                {
                    _audioActive = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(InAudio)));
                }
            }
        }

        public double AudioEnergy
        {
            get { return _audioEnergy; }
            set
            {
                if (_audioEnergy != value)
                {
                    _audioEnergy = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(AudioEnergy)));
                }
            }
        }

        public string ParticipantId => Key;

        #endregion
    }
}
