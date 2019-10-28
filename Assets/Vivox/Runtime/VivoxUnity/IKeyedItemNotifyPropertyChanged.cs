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

namespace VivoxUnity
{
    /// <summary>
    /// Interface for notifying the consumer of changes to an element in a typed collection
    /// </summary>
    /// <typeparam name="TK">The type of the Key</typeparam>
    public interface IKeyedItemNotifyPropertyChanged<out TK> : INotifyPropertyChanged
    {
        /// <summary>
        /// The unique identifier for the element that will raise the property changed event.
        /// </summary>
        TK Key { get; }
    }
}
