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
    /// A read only hash set that raises events when keys are added or removed
    /// </summary>
    /// <typeparam name="T">The type of item in the hashset</typeparam>
    public interface IReadOnlyHashSet<T>
    {
        /// <summary>
        /// Raised after a key is added
        /// </summary>
        event EventHandler<KeyEventArg<T>> AfterKeyAdded;
        /// <summary>
        /// Raised just before a key is removed
        /// </summary>
        event EventHandler<KeyEventArg<T>> BeforeKeyRemoved;

        /// <summary>
        /// Whether a key is contained in the collection
        /// </summary>
        /// <param name="key">key</param>
        /// <returns>true if the key is contained in the collection</returns>
        bool Contains(T key);
        /// <summary>
        /// The number of items in the collection
        /// </summary>
        int Count { get; }
    }
}
