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

namespace VivoxUnity
{
    /// <summary>
    /// Presence information for a user logged in at a particular location
    /// </summary>
    public interface IPresenceLocation : IKeyedItemNotifyPropertyChanged<string>
    {
        /// <summary>
        /// The unique identifier for this account's specific login session. This does not change and therefore will not raise a PropertyChangedEvent.
        /// </summary>
        string LocationId { get; }
        /// <summary>
        /// The presence for this account at this location. This will raise a PropertyChangedEvent when changed.
        /// </summary>
        Presence CurrentPresence { get; }
        /// <summary>
        /// The subscription that owns this presence location. This does not change and therefore will not raise a PropertyChangedEvent.
        /// </summary>
        IPresenceSubscription Subscription { get; }
    }
}
