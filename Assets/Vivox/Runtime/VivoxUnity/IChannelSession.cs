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
using UnityEngine;

namespace VivoxUnity
{
    /// <summary>
    /// A connection to a channel.
    /// </summary>
    public interface IChannelSession : IKeyedItemNotifyPropertyChanged<ChannelId>
    {
        /// <summary>
        /// The login session that owns this channel session.
        /// </summary>
        ILoginSession Parent { get; }

        /// <summary>
        /// The state of the audio portion of this channel session.
        /// </summary>
        /// <remarks>Changes to this value may occur at anytime due to network or moderator events.</remarks>
        ConnectionState AudioState { get; }

        /// <summary>
        /// The state of the text portion of this channel session
        /// </summary>
        /// <remarks>Changes to this value may occur at anytime due to network or moderator events.</remarks>
        ConnectionState TextState { get; }

        /// <summary>
        /// The list of participants in this channel, including the current user.
        /// </summary>
        /// <remarks>Use the IReadOnlyDictionary events to get notifications of participants joining, leaving, or speaking etc.</remarks>
        IReadOnlyDictionary<string, IParticipant> Participants { get; }

        /// <summary>
        /// Whether this user is typing. Setting or clearing this will cause IParticipantProperties.IsTyping to change for other users in the channel.
        /// </summary>
        [Obsolete("This feature is currently not implemented")]
        bool Typing { get; set; }

        /// <summary>
        /// The list of incoming messages.
        /// </summary>
        /// <remarks>Use the IReadOnlyQueue events to get notifications of incoming text messages.</remarks>
        IReadOnlyQueue<IChannelTextMessage> MessageLog { get; }

        /// <summary>
        /// The list of session archive messages returned by a BeginSessionArchiveQuery.
        /// </summary>
        /// <remarks>Use the IReadOnlyQueue events to get notifications of incoming messages from a session archive query.  This is not automatically cleared when starting a new BeginSessionArchiveQuery.</remarks>
        IReadOnlyQueue<ISessionArchiveMessage> SessionArchive { get; }

        /// <summary>
        /// The result set when all the messages have been returned from a BeginSessionArchiveQuery.
        /// </summary>
        /// <remarks>Use the PropertyChanged event to get notified when a session archive query has started or completed.</remarks>
        IArchiveQueryResult SessionArchiveResult { get; }

        /// <summary>
        /// Indicates that this session is the session that is currently transmitting.
        /// </summary>
        /// <remarks>Setting this value to true will clear this value from all other sessions.</remarks>
        bool IsTransmittingSession { get; set; }

        /// <summary>
        /// The channel id of this session.
        /// </summary>
        ChannelId Channel { get; }

        /// <summary>
        /// Perform the initial connection to the channel.
        /// </summary>
        /// <param name="connectAudio">True to connect audio.</param>
        /// <param name="connectText">True to connect text.</param>
        /// <param name="transmit">Whether to transmit in this channel. Transmitting in one channel will stop transmitting in other channels.</param>
        /// <param name="accessToken">The access token granting the user access to the channel.</param>
        /// <param name="callback">A delegate to call when this operation completes.</param>
        /// <returns>The AsyncResult.</returns>
        /// <remarks>
        /// Developer of games that do not have secure communications requirements can use <see cref="GetConnectToken" /> to generate the required access token.
        /// </remarks>
        IAsyncResult BeginConnect(bool connectAudio, bool connectText, TransmitPolicy transmit, string accessToken, AsyncCallback callback);

        /// <summary>
        /// To be called by the consumer of this class when BeginConnect() completes.
        /// </summary>
        /// <param name="result">The IAsyncResult object returned from BeginConnect() or provided to the callback delegate.</param>
        void EndConnect(IAsyncResult result);

        /// <summary>
        /// Disconnect the user from this channel.
        /// </summary>
        /// <remarks>
        /// <param name="callback">A delegate to call when this operation completes.</param>
        /// <returns>The AsyncResult.</returns>
        /// The AudioState and TextState properties will not be set to ConnectionState.Disconnected until it is OK to re-join this channel.  The Application must monitor property changes for these properties in order to determine when it's OK rejoin the channel. This object remains in the ILoginSession.ChannelSessions list. Use ILoginSession.DeleteChannelSession to remove it from the list.
        /// </remarks>
        IAsyncResult Disconnect(AsyncCallback callback = null);

        /// <summary>
        /// Add or remove audio from the channel session.
        /// </summary>
        /// <param name="value">True to add audio, false to remove audio.</param>
        /// <param name="transmit">Whether to transmit in this channel. Transmitting in one channel will stop transmitting in other channels.</param>
        /// <param name="callback">A delegate to call when this operation completes.</param>
        /// <returns>The AsyncResult.</returns>
        IAsyncResult BeginSetAudioConnected(bool value, TransmitPolicy transmit, AsyncCallback callback);

        /// <summary>
        /// To be called by the consumer of this class when BeginSetAudioConnected() completes.
        /// </summary>
        /// <param name="result">The IAsyncResult object returned from BeginSetAudioConnected() or provided to the callback delegate.</param>
        void EndSetAudioConnected(IAsyncResult result);

        /// <summary>
        /// Add or remove text from the channel session.
        /// </summary>
        /// <param name="value">True to add text, false to remove text.</param>
        /// <param name="callback">A delegate to call when this operation completes.</param>
        /// <returns>The AsyncResult.</returns>
        IAsyncResult BeginSetTextConnected(bool value, AsyncCallback callback);

        /// <summary>
        /// To be called by the consumer of this class when BeginSetTextConnected() completes.
        /// </summary>
        /// <param name="result">The IAsyncResult object returned from BeginSetTextConnected() or provided to the callback delegate.</param>
        void EndSetTextConnected(IAsyncResult result);

        /// <summary>
        /// Send a message to this channel.
        /// </summary>
        /// <param name="message">The body of the message.</param>
        /// <param name="callback">A delegate to call when this operation completes.</param>
        /// <returns>The AsyncResult.</returns>
        IAsyncResult BeginSendText(string message, AsyncCallback callback);

        /// <summary>
        /// Send a message to this channel.
        /// </summary>
        /// <param name="language">The language of the message e.g "en". This can be null to use the default language ("en" for most systems). This must conform to RFC5646 (https://tools.ietf.org/html/rfc5646).</param>
        /// <param name="message">The body of the message</param>
        /// <param name="applicationStanzaNamespace">An optional namespace element for additional application data.</param>
        /// <param name="applicationStanzaBody">The additional application data body.</param>
        /// <param name="callback">A delegate to call when this operation completes.</param>
        /// <returns>The AsyncResult.</returns>
        IAsyncResult BeginSendText(string language, string message, string applicationStanzaNamespace, string applicationStanzaBody, AsyncCallback callback);
        
        /// <summary>
        /// To be called by the consumer of this class when BeginSendText() completes.
        /// </summary>
        /// <param name="result">The IAsyncResult object returned from BeginSendText() or provided to the callback delegate.</param>
        void EndSendText(IAsyncResult result);

        /// <summary>
        /// Start a query of archived channel messages.
        /// </summary>
        /// <param name="timeStart">Results filtering: Only messages on or after the given date/time will be returned.  For no start limit, use null.</param>
        /// <param name="timeEnd">Results filtering: Only messages before the given date/time will be returned.  For no end limit, use null.</param>
        /// <param name="searchText">Results filtering: Only messages containing the specified text will be returned.  For order matching, use double-quotes around the search terms.  For no text filtering, use null.</param>
        /// <param name="userId">Results filtering: Only messages to/from the specified participant will be returned.  For no participant filtering, use null.</param>
        /// <param name="max">Results paging: The maximum number of messages to return (up to 50).  If more than 50 messages are needed, multiple queries must be performed.  Use 0 to get total messages count without retrieving them.</param>
        /// <param name="afterId">Results paging: Only messages following the specified message id will be returned in the result set.  If this parameter is set, beforeId must be null.  For no lower limit, use null.</param>
        /// <param name="beforeId">Results paging: Only messages preceding the specified message id will be returned in the result set.  If this parameter is set, afterId must be null.  For no upper limit, use null.</param>
        /// <param name="firstMessageIndex">Results paging: The server side index (not message ID) of the first message to retrieve.  The first message in the result set always has an index of 0.  For no starting message, use -1.</param>
        /// <param name="callback">A delegate to call when this operation completes.</param>
        /// <returns>The AsyncResult.</returns>
        /// <exception cref="ArgumentException">Thrown when max value too large.</exception>
        /// <exception cref="ArgumentException">Thrown when afterId and beforeId used at the same time.</exception>
        IAsyncResult BeginSessionArchiveQuery(DateTime? timeStart, DateTime? timeEnd, string searchText,
            AccountId userId, uint max, string afterId, string beforeId, int firstMessageIndex,
            AsyncCallback callback);

        /// <summary>
        /// To be called by the consumer of this class when BeginSessionArchiveQuery() completes.
        /// </summary>
        /// <param name="result">The IAsyncResult object returned from BeginSessionArchiveQuery() or provided to the callback delegate.</param>
        void EndSessionArchiveQuery(IAsyncResult result);

        /// <summary>
        /// Get a token that can be used to connect to this channel.
        /// </summary>
        /// <param name="tokenSigningKey">The key corresponding to the issuer for this account that is used to sign the token.</param>
        /// <param name="tokenExpirationDuration">The length of time the token is valid for.</param>
        /// <returns>A token that can be used to join this channel.</returns>
        /// <remarks>To be used only by applications without secure communications requirements.</remarks>
        string GetConnectToken(string tokenSigningKey, TimeSpan tokenExpirationDuration);

        /// <summary>
        /// Issues a request to set the listening and speaking positions of a user in a positional channel.
        /// </summary>
        /// <param name="speakerPos">The position of the virtual 'mouth.'</param>
        /// <param name="listenerPos">The position of the virtual 'ear.'</param>
        /// <param name="listenerAtOrient">A unit vector, representing the forward (Z) direction, or heading, of the listener.</param>
        /// <param name="listenerUpOrient">A unit vector, representing the up (Y) direction of the listener. Use Vector3(0, 1, 0) for a 'global' up, in world space.</param>
        void Set3DPosition(Vector3 speakerPos, Vector3 listenerPos, Vector3 listenerAtOrient, Vector3 listenerUpOrient);
    }
}
