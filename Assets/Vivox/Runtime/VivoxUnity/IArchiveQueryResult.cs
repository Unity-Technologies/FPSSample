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

namespace VivoxUnity
{
    /// <summary>
    /// Result of a session or account archive query. 
    /// </summary>
    public interface IArchiveQueryResult
    {
        /// <summary>
        ///  The id of a successfully started query.
        /// </summary>
        string QueryId { get; }
        /// <summary>
        /// The query result code.
        /// </summary>
        int ReturnCode { get; }
        /// <summary>
        /// The query status code.
        /// </summary>
        int StatusCode { get; }
        /// <summary>
        /// The first returned message id.
        /// </summary>
        string FirstId { get; }
        /// <summary>
        /// The last returned message id.
        /// </summary>
        string LastId { get; }
        /// <summary>
        /// The index of the first matching message.
        /// </summary>
        uint FirstIndex { get; }
        /// <summary>
        /// The total number of messages matching the criteria specified in the request.
        /// </summary>
        uint TotalCount { get; }
        /// <summary>
        /// Has the archive query completed?
        /// </summary>
        bool Running { get; }
    }
}
