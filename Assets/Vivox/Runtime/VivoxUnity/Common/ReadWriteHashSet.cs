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
    class ReadWriteHashSet<T> : 
    IReadOnlyHashSet<T>
    {
        private readonly HashSet<T> _items = new HashSet<T>();
        public event EventHandler<KeyEventArg<T>> AfterKeyAdded;
        public event EventHandler<KeyEventArg<T>> BeforeKeyRemoved;

        public bool Contains(T key)
        {
            return _items.Contains(key);
        }

        public bool Add(T key)
        {
            bool added = _items.Add(key);
            if(added)
                AfterKeyAdded?.Invoke(this, new KeyEventArg<T>(key));
            return added;
        }

        public bool Remove(T key)
        {
            if(_items.Contains(key)) 
                BeforeKeyRemoved?.Invoke(this, new KeyEventArg<T>(key));
            return _items.Remove(key);
        }

        public int Count => _items.Count;

        public void Clear()
        {
            _items.Clear();
        }
    }
}
