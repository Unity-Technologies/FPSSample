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
    /// A subscription to another user for online status
    /// </summary>
    public interface IPresenceSubscription : IKeyedItemNotifyPropertyChanged<AccountId>
    {
        /// <summary>
        /// The account that this subscription pertains to.
        /// </summary>
        AccountId SubscribedAccount { get; }
        /// <summary>
        /// If the account associated with this subscription is logged in, the Locations lists will have an entry for each location each login session for that user.
        /// </summary>
        IReadOnlyDictionary<string, IPresenceLocation> Locations { get; }
    }
}
