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
    public class VivoxApiException : Exception
    {
        public int StatusCode { get; private set; }
        public string RequestId { get; private set; }

        public VivoxApiException(int statusCode)
            : base(
                $"{VivoxCoreInstance.vx_get_error_string(statusCode)} ({statusCode})")
        {
            StatusCode = statusCode;
        }
        public VivoxApiException(int statusCode, string requestId)
    : base(
        $"{VivoxCoreInstance.vx_get_error_string(statusCode)} ({statusCode})")
        {
            StatusCode = statusCode;
            RequestId = requestId;
        }

        public VivoxApiException(int statusCode, Exception inner)
            : base(VivoxCoreInstance.vx_get_error_string(statusCode), inner)
        {
        }

        public static string GetErrorString(int statusCode)
        {
            return VivoxCoreInstance.vx_get_error_string(statusCode);
        }
    }

}
