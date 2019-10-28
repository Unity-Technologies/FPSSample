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
    /// The event arguments when an item is added to an IReadOnlyQueue
    /// </summary>
    /// <typeparam name="T">The type of item in the queue</typeparam>
    public sealed class QueueItemAddedEventArgs<T> : EventArgs
    {
        /// <summary>
        /// constructor
        /// </summary>
        /// <param name="item">The item added</param>
        public QueueItemAddedEventArgs(T item)
        {
            Value = item;
        }

        /// <summary>
        /// The value 
        /// </summary>
        public T Value { get; }
    }

    /// <summary>
    /// A queue that raises an event when an item is added.
    /// </summary>
    /// <typeparam name="T">The type of item in the queue</typeparam>
    public interface IReadOnlyQueue<T>
    {
        /// <summary>
        /// Event that is raised when an item is added
        /// </summary>
        event EventHandler<QueueItemAddedEventArgs<T>> AfterItemAdded;

        /// <summary>
        /// remove an item from the queue.
        /// </summary>
        /// <returns>The item, or null if the queue is empty</returns>
        T Dequeue();
        /// <summary>
        /// Remove all the items from the queue
        /// </summary>
        void Clear();
        /// <summary>
        /// Count of items in the queue
        /// </summary>

        int Count { get; }
        /// <summary>
        /// Look at the head of the queue without dequeuing
        /// </summary>
        /// <returns>the next item in the queue. Null if queue is empty.</returns>
        T Peek();
    }
}
