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
using System.Collections.Generic;

namespace VivoxUnity
{
    /// <summary>
    /// Event arguments for key collections with add/delete notifications
    /// </summary>
    /// <typeparam name="TK">The Key Type</typeparam>
    public sealed class KeyEventArg<TK> : EventArgs {
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="key">The key that uniquely identifies the item in the collection</param>
        public KeyEventArg(TK key)
        {
            Key = key;
        }
        /// <summary>
        /// The key that uniquely identifies the item in the collection
        /// </summary>
        public TK Key { get; set; }
    }

    /// <summary>
    /// Event Arguments for key collections with value changed notifications
    /// </summary>
    /// <typeparam name="TK">The Key Type</typeparam>
    /// <typeparam name="TV">The Value Type</typeparam>
    public sealed class ValueEventArg<TK, TV> : EventArgs {
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="key">The key that uniquely identifies the item in the collection</param>
        /// <param name="value">A value that maps to that key</param>
        /// <param name="name">The name of the changed property</param>
        public ValueEventArg(TK key, TV value, string name)
        {
            Key = key;
            Value = value;
            PropertyName = name;
        }
        /// <summary>
        /// The key that uniquely identifies the item in the collection
        /// </summary>
        public TK Key { get; set; }
        /// <summary>
        /// A value that maps to a key
        /// </summary>
        public TV Value { get; set; }
        /// <summary>
        /// The name of the changed property
        /// </summary>
        public string PropertyName { get; set; }
    }

    /// <summary>
    /// A dictionary that is read only, and will raise an event whenever an item is added, removed, or modified
    /// </summary>
    /// <typeparam name="TK">The Key Type</typeparam>
    /// <typeparam name="T">The Value Type</typeparam>
    public interface IReadOnlyDictionary<TK, T> : IEnumerable<T>
    {
        /// <summary>
        /// Raised when an item is added
        /// </summary>
        event EventHandler<KeyEventArg<TK>> AfterKeyAdded;
        /// <summary>
        /// Raised when an item is about to be removed
        /// </summary>
        event EventHandler<KeyEventArg<TK>> BeforeKeyRemoved;
        /// <summary>
        /// Raised after a value has been updated
        /// </summary>
        event EventHandler<ValueEventArg<TK, T>> AfterValueUpdated;

        /// <summary>
        /// All the keys
        /// </summary>
        ICollection<TK> Keys { get; }
        /// <summary>
        /// The value for a particular key
        /// </summary>
        /// <param name="key"></param>
        /// <returns>The value</returns>
        T this[TK key] { get;  }

        /// <summary>
        /// Determines if key is contained
        /// </summary>
        /// <param name="key">the key</param>
        /// <returns>true of the key is contained</returns>
        bool ContainsKey(TK key);

        /// <summary>
        /// The number of items in the collection
        /// </summary>
        int Count { get; }
    }
}
