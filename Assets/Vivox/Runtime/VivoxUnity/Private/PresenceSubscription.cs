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

using System.ComponentModel;
using VivoxUnity.Common;

namespace VivoxUnity.Private
{
    internal class PresenceSubscription : IPresenceSubscription
    {
        private readonly ReadWriteDictionary<string, IPresenceLocation, PresenceLocation> _locations = new ReadWriteDictionary<string, IPresenceLocation, PresenceLocation>();
#pragma warning disable 0067
        public event PropertyChangedEventHandler PropertyChanged;
#pragma warning restore 0067
        public AccountId Key { get; set; }
        public IReadOnlyDictionary<string, IPresenceLocation> Locations => _locations;

        public void UpdateLocation(string uriWithTag, PresenceStatus status, string message)
        {
            PresenceLocation item;
            if (!_locations.ContainsKey(uriWithTag))
            {
                if (status != PresenceStatus.Unavailable)
                {
                    item = new PresenceLocation(uriWithTag)
                    {
                        CurrentPresence = new Presence(status, message),
                        Subscription = this
                    };
                    _locations[item.Key] = item;
                }
            }
            else
            {
                item = (PresenceLocation)_locations[uriWithTag];
                item.CurrentPresence = new Presence(status, message);
                if (status == PresenceStatus.Unavailable)
                {
                    _locations.Remove(uriWithTag);
                }
            }
        }

        public AccountId SubscribedAccount => Key;
    }
}
