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
    /// A participant in a channel
    /// 
    /// <remarks>
    /// Note that the key for this interface is not the account id. It's a unique identifier of that participant's session in that channel
    /// </remarks>
    /// </summary>
    public interface IParticipant : IKeyedItemNotifyPropertyChanged<string>, IParticipantProperties
    {
        /// <summary>
        /// The unique identifier for this participant.
        /// <remarks>This is not the same as <see cref="Account"/></remarks>
        /// </summary>
        string ParticipantId { get; }

        /// <summary>
        /// The ChannelSession that owns this Participant
        /// </summary>
        IChannelSession ParentChannelSession { get; }
        /// <summary>
        /// The account id of this particiapnt
        /// </summary>
        AccountId Account { get; }
        /// <summary>
        /// Used to mute a given user for all other users in a channel
        /// </summary>
        /// <param name="accountHandle">The account handle of the user you are muting.</param>
        /// <param name="setMuted">Set to true to mute or false to unmute.</param>
        /// <param name="accessToken">The access token granting the user permission to mute this participant in the channel.</param>
        /// <param name="callback">A delegate to call when this operation completes.</param>
        /// <returns>The AsyncResult.</returns>
        IAsyncResult SetIsMuteForAll(string accountHandle, bool setMuted, string accessToken, AsyncCallback callback);
    }
}
