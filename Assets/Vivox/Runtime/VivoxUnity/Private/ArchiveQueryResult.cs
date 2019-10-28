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

namespace VivoxUnity.Private
{
    internal class ArchiveQueryResult : IArchiveQueryResult
    {
        public string QueryId { get; private set; }
        public int ReturnCode { get; private set; }
        public int StatusCode { get; private set; }
        public string FirstId { get; private set; }
        public string LastId { get; private set; }
        public uint FirstIndex { get; private set; }
        public uint TotalCount { get; private set; }
        public bool Running { get; private set; }

        /// <summary>
        /// Constructor for no query.
        /// </summary>
        public ArchiveQueryResult()
        {
            QueryId = "";
            ReturnCode = -1;
            StatusCode = -1;
            FirstId = "";
            LastId = "";
            FirstIndex = 0;
            TotalCount = 0;
            Running = false;
        }

        /// <summary>
        /// Constructor for uncompleted query.
        /// </summary>
        public ArchiveQueryResult(string queryId)
        {
            QueryId = queryId;
            ReturnCode = -1;
            StatusCode = -1;
            FirstId = "";
            LastId = "";
            FirstIndex = 0;
            TotalCount = 0;
            Running = true;
        }

        /// <summary>
        /// Constructor for completed session archive query.
        /// </summary>
        public ArchiveQueryResult(vx_evt_session_archive_query_end_t evt)
        {
            QueryId = evt.query_id;
            ReturnCode = evt.return_code;
            StatusCode = evt.status_code;
            FirstId = evt.first_id;
            LastId = evt.last_id;
            FirstIndex = evt.first_index;
            TotalCount = evt.count;
            Running = false;
        }

        /// <summary>
        /// Constructor for completed account archive query.
        /// </summary>
        public ArchiveQueryResult( vx_evt_account_archive_query_end_t evt)
        {
            QueryId = evt.query_id;
            ReturnCode = evt.return_code;
            StatusCode = evt.status_code;
            FirstId = evt.first_id;
            LastId = evt.last_id;
            FirstIndex = evt.first_index;
            TotalCount = evt.count;
            Running = false;
        }
    }
}
