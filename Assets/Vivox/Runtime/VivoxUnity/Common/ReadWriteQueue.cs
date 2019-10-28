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

namespace VivoxUnity.Common
{
    public sealed class ReadWriteQueue<T> : IReadOnlyQueue<T>
    {
        private readonly List<T> _items = new List<T>();
        public event EventHandler<QueueItemAddedEventArgs<T> > AfterItemAdded;
        public T Dequeue()
        {
            if (_items.Count == 0)
                return default(T);
            var item = _items[0];
            _items.RemoveAt(0);
            return item;
        }

        public void Clear()
        {
            _items.Clear();
        }

        public int Count => _items.Count;

        public T Peek()
        {
            if (_items.Count == 0)
                return default(T);
            return _items[0];
        }

        public void Enqueue(T item)
        {
            _items.Add(item);
            AfterItemAdded?.Invoke(this, new QueueItemAddedEventArgs<T>(item));
        }

        public bool Contains(T item)
        {
            foreach (var i in _items)
            {
                if (i.Equals(item))
                    return true;
            }
            return false;
        }

        public int RemoveAll(T item)
        {
            return _items.RemoveAll(i => i.Equals(item));
        }
    }
}
