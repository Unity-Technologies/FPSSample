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


namespace VivoxUnity
{

    /// <summary>
    /// A session for an account.
    /// </summary>
    public interface ILoginSession : IKeyedItemNotifyPropertyChanged<AccountId>
    {
        #region Properties
        /// <summary>
        /// The list of channel sessions associated with this login session.
        /// </summary>
        IReadOnlyDictionary<ChannelId, IChannelSession> ChannelSessions { get; }
        /// <summary>
        /// The list of presence subscriptions associated with this login session.
        /// </summary>
        /// <remarks>This typically corresponds to a list of "friends".</remarks>
        IReadOnlyDictionary<AccountId, IPresenceSubscription> PresenceSubscriptions { get; }
        /// <summary>
        /// The list of accounts blocked from seeing this accounts online status.
        /// </summary>
        IReadOnlyHashSet<AccountId> BlockedSubscriptions { get; }
        /// <summary>
        /// The list of accounts allowed to see this accounts online status.
        /// </summary>
        IReadOnlyHashSet<AccountId> AllowedSubscriptions { get; }
        /// <summary>
        /// The list of incoming subscription requests.
        /// </summary>
        IReadOnlyQueue<AccountId> IncomingSubscriptionRequests { get; }
        /// <summary>
        /// The list of incoming user to user messages.
        /// </summary>
        IReadOnlyQueue<IDirectedTextMessage> DirectedMessages { get; }
        /// <summary>
        /// The list of failed user to user messages.
        /// </summary>
        IReadOnlyQueue<IFailedDirectedTextMessage> FailedDirectedMessages { get; }
        /// <summary>
        /// The list of account archive messages returned by a BeginAccountArchiveQuery.
        /// </summary>
        /// <remarks>Use the IReadOnlyQueue events to get notifications of incoming messages from a account archive query.  This is not automatically cleared when starting a new BeginAccountArchiveQuery.</remarks>
        IReadOnlyQueue<IAccountArchiveMessage> AccountArchive { get; }
        /// <summary>
        /// The result set when all the messages have been returned from a BeginAccountArchiveQuery.
        /// </summary>
        /// <remarks>Use the PropertyChanged event to get notified when a account archive query has started or completed.</remarks>
        IArchiveQueryResult AccountArchiveResult { get; }
        /// <summary>
        /// The result set when a user to user message has been sent.
        /// </summary>
        /// <remarks>Use the PropertyChanged event to get notified when a directed message has been sent.</remarks>
        IDirectedMessageResult DirectedMessageResult { get; }
        /// <summary>
        /// The current state of this login session.
        /// </summary>
        LoginState State { get; }
        /// <summary>
        /// The current status of injected audio.
        /// </summary>
        bool IsInjectingAudio { get; }
        /// <summary>
        /// The online status that is sent to those accounts subscribing to the presence of this account.
        /// </summary>
        Presence Presence { get; set; }
        /// <summary>The unique identifier for this LoginSession.</summary>
        AccountId LoginSessionId { get; }
        /// <summary>
        /// Specifies how often the SDK will send participant property events while in a channel.
        /// </summary>
        /// <remarks>
        /// <para>Only use this property to set the update frequency before login.  After login, the <see cref="BeginAccountSetLoginProperties" /> call must be used.</para>
        /// <para>Participant property events by default are only sent on participant state change (starts talking, stops talking, is muted, is unmuted). If set to a per second rate, messages will be sent at that rate if there has been a change since the last update message. This is always true unless the participant is muted through the SDK, causing no audio energy and no state changes.</para>
        /// <para>WARNING: Setting this value a non-default value will increase user and server traffic. It should only be done if a real-time visual representation of audio values are needed (e.g., graphic VAD indicator). For a static VAD indicator, the default setting is correct.</para>
        /// </remarks>
        ParticipantPropertyUpdateFrequency ParticipantPropertyFrequency { get; set; }
        #endregion
        #region Methods

        /// <summary>
        /// Begin logging in this session when presence (subscriptions) are desired.
        /// </summary>
        /// <param name="server">The URI of the Vivox instance assigned to you.</param>
        /// <param name="accessToken">an access token provided by your game server that enables this login.</param>
        /// <param name="subscriptionMode">how to handle incoming subscriptions.</param>
        /// <param name="presenceSubscriptions">A list of accounts for which this user wishes to monitor online status.</param>
        /// <param name="blockedPresenceSubscriptions">A list of accounts that are not allwed to see this user's online status.</param>
        /// <param name="allowedPresenceSubscriptions">A list of accounts that are allowed to see this user's online status.</param>
        /// <param name="callback">a delegate to call when this operation completes.</param>
        /// <returns>IAsyncResult</returns>
        /// <remarks>
        /// Developer of games that do not have secure communications requirements can use <see cref="GetLoginToken" /> to generate the required access token.
        /// </remarks>
        IAsyncResult BeginLogin(
            Uri server,
            string accessToken,
            SubscriptionMode subscriptionMode,
            IReadOnlyHashSet<AccountId> presenceSubscriptions,
            IReadOnlyHashSet<AccountId> blockedPresenceSubscriptions,
            IReadOnlyHashSet<AccountId> allowedPresenceSubscriptions,
            AsyncCallback callback);

        /// <summary>
        /// Begin logging in this session for text and voice only (no subscriptions possible).
        /// </summary>
        /// <param name="server">The URI of the Vivox instance assigned to you</param>
        /// <param name="accessToken">an access token provided by your game server that enables this login</param>
        /// <param name="callback">a delegate to call when this operation completes</param>
        /// <returns>IAsyncResult</returns>
        /// <remarks>
        /// Developer of games that do not have secure communications requirements can use <see cref="GetLoginToken" /> to generate the required access token.
        /// </remarks>
        IAsyncResult BeginLogin(
            Uri server,
            string accessToken,
            AsyncCallback callback);

        /// <summary>
        /// To be called by the consumer of this class when BeginLogin() completes.
        /// </summary>
        /// <param name="result">The IAsyncResult object returned from BeginLogin() or provided to the callback delegate</param>
        void EndLogin(IAsyncResult result);

        /// <summary>
        /// Called to change login properties when already logged in
        /// </summary>
        /// <param name="participantPropertyFrequency">How often the SDK will send participant property events while in a channel</param>
        /// <param name="callback">A delegate to call when this operation completes</param>
        /// <returns>IAsyncResult</returns>    
        /// <remarks>
        /// <para>Only use this property to set the update frequency after login.  Before login, the <see cref="ParticipantPropertyFrequency" /> property must be used.</para>
        /// <para>Participant property events by default are only sent on participant state change (starts talking, stops talking, is muted, is unmuted). If set to a per second rate, messages will be sent at that rate if there has been a change since the last update message. This is always true unless the participant is muted through the SDK, causing no audio energy and no state changes.</para>
        /// <para>WARNING: Setting this value a non-default value will increase user and server traffic. It should only be done if a real-time visual representation of audio values are needed (e.g., graphic VAD indicator). For a static VAD indicator, the default setting is correct.</para>
        /// </remarks>
        IAsyncResult BeginAccountSetLoginProperties(ParticipantPropertyUpdateFrequency participantPropertyFrequency, AsyncCallback callback);
        /// <summary>
        /// To be called by the consumer of this class when BeginAccountSetLoginProperties() completes.
        /// </summary>
        /// <param name="result">The IAsyncResult object returned from BeginAccountSetLoginProperties() or provided to the callback delegate</param>
        void EndAccountSetLoginProperties(IAsyncResult result);

        /// <summary>
        /// Gets the channel session for this channelId, creating one if necessary
        /// </summary>
        /// <param name="channelId">the id of the channel</param>
        /// <returns>the channel session</returns>
        IChannelSession GetChannelSession(ChannelId channelId);
        /// <summary>
        /// Deletes the channel session for this channelId, disconnecting the session if necessary
        /// </summary>
        /// <param name="channelId">the id of the channel</param>
        void DeleteChannelSession(ChannelId channelId);

        /// <summary>
        /// Block incoming subscription requests from the specified account
        /// </summary>
        /// <param name="accountId">the account id to block</param>
        /// <param name="callback">a delegate to call when this operation completes</param>
        /// <returns>IAsyncResult</returns>
        IAsyncResult BeginAddBlockedSubscription(AccountId accountId, AsyncCallback callback);
        /// <summary>
        /// To be called by the consumer of this class when BeginAddBlockedSubscription() completes.
        /// </summary>
        /// <param name="result">The IAsyncResult object returned from BeginAddBlockedSubscription() or provided to the callback delegate</param>
        void EndAddBlockedSubscription(IAsyncResult result);
        /// <summary>
        /// Unblock incoming subscription requests from the specified account. Subscription requests from the specified account will cause an event to be raised to the application.
        /// </summary>
        /// <param name="accountId">the account id to unblock</param>
        /// <param name="callback">a delegate to call when this operation completes</param>
        /// <returns>IAsyncResult</returns>
        IAsyncResult BeginRemoveBlockedSubscription(AccountId accountId, AsyncCallback callback);
        /// <summary>
        /// To be called by the consumer of this class when BeginRemoveBlockedSubscription() completes.
        /// </summary>
        /// <param name="result">The IAsyncResult object returned from BeginRemoveBlockedSubscription() or provided to the callback delegate</param>
        void EndRemoveBlockedSubscription(IAsyncResult result);

        /// <summary>
        /// Allow incoming subscription requests from the specified account
        /// </summary>
        /// <param name="accountId">the account id to allow</param>
        /// <param name="callback">a delegate to call when this operation completes</param>
        /// <returns>IAsyncResult</returns>
        IAsyncResult BeginAddAllowedSubscription(AccountId accountId, AsyncCallback callback);
        /// <summary>
        /// To be called by the consumer of this class when BeginAddAllowedSubscription() completes.
        /// </summary>
        /// <param name="result">The IAsyncResult object returned from BeginAddAllowedSubscription() or provided to the callback delegate</param>
        void EndAddAllowedSubscription(IAsyncResult result);
        /// <summary>
        /// Disallow incoming subscription requests from the specified account. Subscription requests from the specified account will cause an event to be raised to the application.
        /// </summary>
        /// <param name="accountId">the account id to disallow</param>
        /// <param name="callback">a delegate to call when this operation completes</param>
        /// <returns>IAsyncResult</returns>
        IAsyncResult BeginRemoveAllowedSubscription(AccountId accountId, AsyncCallback callback);
        /// <summary>
        /// To be called by the consumer of this class when BeginRemoveAllowedSubscription() completes.
        /// </summary>
        /// <param name="result">The IAsyncResult object returned from BeginRemoveAllowedSubscription() or provided to the callback delegate</param>
        void EndRemoveAllowedSubscription(IAsyncResult result);
        /// <summary>
        /// Subscribe to the specified account
        /// </summary>
        /// <param name="accountId">the account id to subscribe to</param>
        /// <param name="callback">a delegate to call when this operation completes</param>
        /// <remarks>This method will automatically allow accountId to see the subscriber's online status</remarks>
        /// <returns>IAsyncResult</returns>
        IAsyncResult BeginAddPresenceSubscription(AccountId accountId, AsyncCallback callback);
        /// <summary>
        /// To be called by the consumer of this class when BeginAddPresenceSubscription() completes.
        /// </summary>
        /// <param name="result">The IAsyncResult object returned from BeginAddPresenceSubscription() or provided to the callback delegate</param>
        /// <returns>The presence subscription for the account id</returns>
        IPresenceSubscription EndAddPresenceSubscription(IAsyncResult result);
        /// <summary>
        /// Unsubscribe from the specified account
        /// </summary>
        /// <param name="accountId">the account id to subscribe to</param>
        /// <param name="callback">a delegate to call when this operation completes</param>
        /// <returns>IAsyncResult</returns>
        IAsyncResult BeginRemovePresenceSubscription(AccountId accountId, AsyncCallback callback);
        /// <summary>
        /// To be called by the consumer of this class when BeginRemovePresenceSubscription() completes.
        /// </summary>
        /// <param name="result">The IAsyncResult object returned from BeginRemovePresenceSubscription() or provided to the callback delegate</param>
        void EndRemovePresenceSubscription(IAsyncResult result);


        /// <summary>
        /// Send a message to the specific account
        /// </summary>
        /// <param name="accountId">the intended recipient of the message</param>
        /// <param name="language">the language of the message e.g "en". This can be null to use the default language ("en" for most systems). This must conform to RFC5646 (https://tools.ietf.org/html/rfc5646)</param>
        /// <param name="message">the body of the message</param>
        /// <param name="applicationStanzaNamespace">an optional namespace element for additional application data</param>
        /// <param name="applicationStanzaBody">the additional application data body</param>
        /// <param name="callback">a delegate to call when this operation completes</param>
        /// <returns>The AsyncResult</returns>
        IAsyncResult BeginSendDirectedMessage(AccountId accountId, string language, string message, string applicationStanzaNamespace, string applicationStanzaBody, AsyncCallback callback);

        /// <summary>
        /// Send a message to the specific account
        /// </summary>
        /// <param name="accountId">the intended recipient of the message</param>
        /// <param name="message">the body of the message</param>
        /// <param name="callback">a delegate to call when this operation completes</param>
        /// <returns>The AsyncResult</returns>
        IAsyncResult BeginSendDirectedMessage(AccountId accountId, string message, AsyncCallback callback);
        /// <summary>
        /// To be called by the consumer of this class when BeginSendDirectedMessage() completes.
        /// </summary>
        /// <param name="result">The IAsyncResult object returned from BeginSendDirectedMessage() or provided to the callback delegate</param>
        void EndSendDirectedMessage(IAsyncResult result);

        /// <summary>
        /// Start a query of archived directed messages.
        /// </summary>
        /// <param name="timeStart">Results filtering: Only messages on or after the given date/time will be returned.  For no start limit, use null.</param>
        /// <param name="timeEnd">Results filtering: Only messages before the given date/time will be returned.  For no end limit, use null.</param>
        /// <param name="searchText">Results filtering: Only messages containing the specified text will be returned.  For order matching, use double-quotes around the search terms.  For no text filtering, use null.</param>
        /// <param name="userId">Results filtering: Only messages to/from the specified participant will be returned.  If this parameter is set, channel must be null.  For no participant filtering, use null.</param>
        /// <param name="channel">Results filtering: Only messages to/from the specified channel will be returned.  If this parameter is set, userId must be null.  For no channel filtering, use null.</param>
        /// <param name="max">Results paging: The maximum number of messages to return (up to 50).  If more than 50 messages are needed, multiple queries must be performed.  Use 0 to get total messages count without retrieving them.</param>
        /// <param name="afterId">Results paging: Only messages following the specified message id will be returned in the result set.  If this parameter is set, beforeId must be null.  For no lower limit, use null.</param>
        /// <param name="beforeId">Results paging: Only messages preceding the specified message id will be returned in the result set.  If this parameter is set, afterId must be null.  For no upper limit, use null.</param>
        /// <param name="firstMessageIndex">Results paging: The server side index (not message ID) of the first message to retrieve.  The first message in the result set always has an index of 0.  For no starting message, use -1.</param>
        /// <param name="callback">A delegate to call when this operation completes.</param>
        /// <returns>The AsyncResult.</returns>
        /// <exception cref="ArgumentException">Thrown when max value too large.</exception>
        /// <exception cref="ArgumentException">Thrown when afterId and beforeId are used at the same time.</exception>
        /// <exception cref="ArgumentException">Thrown when userId and channel are used at the same time.</exception>
        IAsyncResult BeginAccountArchiveQuery(DateTime? timeStart, DateTime? timeEnd, string searchText,
            AccountId userId, ChannelId channel, uint max, string afterId, string beforeId, int firstMessageIndex,
            AsyncCallback callback);
        /// <summary>
        /// To be called by the consumer of this class when BeginArchiveArchiveQuery() completes.
        /// </summary>
        /// <param name="result">The IAsyncResult object returned from BeginAccountArchiveQuery() or provided to the callback delegate.</param>
        void EndAccountArchiveQuery(IAsyncResult result);

        /// <summary>
        /// Log the account out of the Vivox system. This will not raise a property event for changing the State to LoginState.LoggedOut, although 
        /// the state will be set to that upon completion of the function.
        /// </summary>
        void Logout();

        /// <summary>
        /// Get a login token for this account. 
        /// </summary>
        /// <param name="tokenSigningKey">the key corresponding to the issuer for this account that is used to sign the token</param>
        /// <param name="tokenExpirationDuration">the length of time the token is valid for.</param>
        /// <returns>an access token that can be used to log this account in</returns>
        /// <remarks>To be used only by applications without secure communications requirements.</remarks>
        string GetLoginToken(string tokenSigningKey, TimeSpan tokenExpirationDuration);

        /// <summary>
        /// This function allows you to start audio injection
        /// </summary>
        /// <param name="audioFilePath">The full pathname for the WAV file to use for audio injection (MUST be single channel, 16-bit PCM, with the same sample rate as the negotiated audio codec) required for start</param>
        void StartAudioInjection(string audioFilePath);

        /// <summary>
        /// This function allows you to stop audio injection
        /// </summary>
        void StopAudioInjection();
        #endregion
    }
}
