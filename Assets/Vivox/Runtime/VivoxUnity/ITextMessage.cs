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

namespace VivoxUnity
{
    /// <summary>
    /// A Text Message
    /// </summary>
    public interface ITextMessage : IKeyedItemNotifyPropertyChanged<string>
    {
        /// <summary>
        /// The time that the message was received.
        /// </summary>
        DateTime ReceivedTime { get; }
        /// <summary>
        /// The message.
        /// </summary>
        string Message { get; }
        /// <summary>
        /// The language of the Message.
        /// </summary>
        string Language { get; }
    }

    /// <summary>
    /// A text message from one user to another user.
    /// </summary>
    public interface IDirectedTextMessage : ITextMessage
    {
        /// <summary>
        /// The LoginSession that is the target of the message.
        /// </summary>
        ILoginSession LoginSession { get; }
        /// <summary>
        /// The sender.
        /// </summary>
        AccountId Sender { get; }
        /// <summary>
        /// An optional name space for application specific data.
        /// </summary>
        string ApplicationStanzaNamespace { get; }
        /// <summary>
        /// Optional application specific data.
        /// </summary>
        string ApplicationStanzaBody { get; }
    }

    /// <summary>
    /// A text message from one user to another user.
    /// </summary>
    public interface IFailedDirectedTextMessage
    {
        /// <summary>
        /// The sender.
        /// </summary>
        AccountId Sender { get; }
        /// <summary>
        /// The request ID of the failed directed message.
        /// </summary>
        string RequestId { get; }
        /// <summary>
        /// The status code of the failure.
        /// </summary>
        int StatusCode { get; }
    }

    /// <summary>
    /// A text message from a channel.
    /// </summary>
    public interface IChannelTextMessage : ITextMessage
    {
        /// <summary>
        /// The ChannelSesssion that is the target of the message.
        /// </summary>
        IChannelSession ChannelSession { get; }
        /// <summary>
        /// The sender.
        /// </summary>
        AccountId Sender { get; }
        /// <summary>
        /// The message is from the current logged in user.
        /// </summary>
        bool FromSelf { get; }
        /// <summary>
        /// An optional name space for application specific data.
        /// </summary>
        string ApplicationStanzaNamespace { get; }
        /// <summary>
        /// Optional application specific data.
        /// </summary>
        string ApplicationStanzaBody { get; }
    }

    /// <summary>
    /// A text message from a session archive query.
    /// </summary>
    public interface ISessionArchiveMessage : ITextMessage
    {
        /// <summary>
        /// The ChannelSesssion that is the target of the message.
        /// </summary>
        IChannelSession ChannelSession { get; }
        /// <summary>
        /// The sender.
        /// </summary>
        AccountId Sender { get; }
        /// <summary>
        /// The message is from the current logged in user.
        /// </summary>
        bool FromSelf { get; }
        /// <summary>
        /// The id of the query that requested this message.
        /// </summary>
        string QueryId { get; }
        /// <summary>
        /// The server-assigned id of the message used for paging through the large result sets.
        /// </summary>
        string MessageId { get; }
    }

    /// <summary>
    /// A text message from an account archive query.
    /// </summary>
    public interface IAccountArchiveMessage : ITextMessage
    {
        /// <summary>
        /// The LoginSession that is the sender/receiver of this message.
        /// </summary>
        ILoginSession LoginSession { get; }
        /// <summary>
        /// The id of the query that requested this message.
        /// </summary>
        string QueryId { get; }
        /// <summary>
        /// If a direct message, the remote participant who is the sender/receiver of the message for inbound/outbound messages respectively.  If this is a channel message, this value will be null.
        /// </summary>
        AccountId RemoteParticipant { get; }
        /// <summary>
        /// If a channel message, the channel that is the sender/receiver of the message for inbound/outbound messages respectively.  If this is a direct message, this value will be null.
        /// </summary>
        ChannelId Channel { get; }
        /// <summary>
        /// The message direction: true for inbound and false for outbound.
        /// </summary>
        bool Inbound { get; }
        /// <summary>
        /// The server-assigned id of the message used for paging through the large result sets.
        /// </summary>
        string MessageId { get; }
    }
}
