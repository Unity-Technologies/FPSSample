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

namespace VivoxUnity.Private
{
    internal class DirectedTextMessage : IDirectedTextMessage
    {
        private Exception _exception;
        public event PropertyChangedEventHandler PropertyChanged;
        public Exception Exception
        {
            get { return _exception; }
            set
            {
                if (_exception != value)
                {
                    _exception = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Exception)));
                }
            }
        }
        public string Key { get; set; }

        public DateTime ReceivedTime { get; set; }
        public string Message { get; set; }
        public string Language { get; set; }

        public ILoginSession LoginSession { get; set; }
        public AccountId Sender { get; set; }
        public string ApplicationStanzaNamespace { get; set; }
        public string ApplicationStanzaBody { get; set; }
    }

    internal class FailedDirectedTextMessage : IFailedDirectedTextMessage
    {
        private Exception _exception;
        public event PropertyChangedEventHandler PropertyChanged;
        public Exception Exception
        {
            get { return _exception; }
            set
            {
                if (_exception != value)
                {
                    _exception = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Exception)));
                }
            }
        }

        public AccountId Sender { get; set; }
        public string RequestId { get; set; }
        public int StatusCode { get; set; }
    }

    internal class ChannelTextMessage : IChannelTextMessage
    {
        private Exception _exception;
        public event PropertyChangedEventHandler PropertyChanged;
        public Exception Exception
        {
            get { return _exception; }
            set
            {
                if (_exception != value)
                {
                    _exception = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Exception)));
                }
            }
        }
        public string Key { get; set; }

        public DateTime ReceivedTime { get; set; }
        public string Message { get; set; }
        public string Language { get; set; }

        public IChannelSession ChannelSession { get; set; }
        public AccountId Sender { get; set; }
        public bool FromSelf { get; set; }
        public string ApplicationStanzaNamespace { get; set; }
        public string ApplicationStanzaBody { get; set; }
     }

    internal class SessionArchiveMessage : ISessionArchiveMessage
    {
        private Exception _exception;
        public event PropertyChangedEventHandler PropertyChanged;
        public Exception Exception
        {
            get { return _exception; }
            set
            {
                if (_exception != value)
                {
                    _exception = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Exception)));
                }
            }
        }
        public string Key { get; set; }

        public DateTime ReceivedTime { get; set; }
        public string Message { get; set; }
        public string Language { get; set; }

        public IChannelSession ChannelSession { get; set; }
        public AccountId Sender { get; set; }
        public bool FromSelf { get; set; }

        public string QueryId { get; set; }
        public string MessageId { get; set; }
    }

    public class AccountArchiveMessage : IAccountArchiveMessage
    {
        private Exception _exception;
        public event PropertyChangedEventHandler PropertyChanged;
        public Exception Exception
        {
            get { return _exception; }
            set
            {
                if (_exception != value)
                {
                    _exception = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Exception)));
                }
            }
        }
        public string Key { get; set; }

        public DateTime ReceivedTime { get; set; }
        public string Message { get; set; }
        public string Language { get; set; }

        public ILoginSession LoginSession { get; set; }
        public string QueryId { get; set; }
        public AccountId RemoteParticipant { get; set; }
        public ChannelId Channel { get; set; }
        public bool Inbound { get; set; }
        public string MessageId { get; set; }
    }
}
