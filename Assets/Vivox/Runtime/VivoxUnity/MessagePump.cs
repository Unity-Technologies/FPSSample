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
using System.Linq;
using System.Text;
using System.Threading;

    public delegate bool LoopDone();

    public delegate void RunLoop(ref bool didWork);


    public class Waiter
    {
        private WaitHandle _waitHandle;
        private DateTime _until;
        public Waiter(WaitHandle handle, TimeSpan until)
        {
            _waitHandle = handle;
            _until = DateTime.Now + until;
        }

        public bool IsDone()
        {
            if (_waitHandle != null)
            {
                if (_waitHandle.WaitOne(0))
                    return true;
            }
            if (DateTime.Now >= _until)
                return true;
            return false;
        }
    }

    public class MessagePump
    {
        private static MessagePump _instance;
        MessagePump()
        {
        }

        public event RunLoop MainLoopRun;

        public static MessagePump Instance
        {
            get
            {
                if (_instance == null)
                    _instance = new MessagePump();
                return _instance;
            }
        }



        public void RunUntil(LoopDone done)
        {
            for(;;)
            {
                bool didWork = false;
                MainLoopRun?.Invoke(ref didWork);
                if (didWork)
                    continue;
                if (!done())
                {
                    Thread.Sleep(20);
                }
                else
                {
                    break;
                }
            } 
        }

        public void RunOnce()
        {
            for (;;)
            {
                bool didWork = false;
                MainLoopRun?.Invoke(ref didWork);
                if (didWork)
                    continue;
                break;
            }
        }

        public static bool IsDone(WaitHandle handle, DateTime until)
        {
            if (handle != null)
            {
                if (handle.WaitOne(0))
                    return true;
            }
            if (DateTime.Now >= until)
                return true;
            return false;
        }

        public static bool Run(WaitHandle handle, TimeSpan until)
        {
            var then = DateTime.Now + until;
            MessagePump.Instance.RunUntil(new LoopDone(() => MessagePump.IsDone(handle, then)));
            if (handle != null) return handle.WaitOne(0);
            return false;
        }

        public delegate bool DoneDelegate();

        //public static void RunUntil(DoneDelegate done)
        //{
        //    Instance.RunUntil(new LoopDone(() => done()));
        //}
}
