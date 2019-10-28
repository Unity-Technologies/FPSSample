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

namespace VivoxUnity
{
    /// <summary>
    /// The presence information for a user at location
    /// </summary>
    public struct Presence
    {
        /// <summary>
        /// The online status of the user
        /// </summary>
        public readonly PresenceStatus Status;
        /// <summary>
        /// An option message published by that user
        /// </summary>
        public readonly string Message;

        /// <summary>
        /// constructor
        /// </summary>
        /// <param name="status">The online status of the user</param>
        /// <param name="message">An optional message</param>
        public Presence(PresenceStatus status, string message)
        {
            Status = status;
            Message = message;
        }

        /// <summary>
        /// Determine if two objects are equal
        /// </summary>
        /// <param name="obj">the other object</param>
        /// <returns>true if objects equal</returns>
        public override bool Equals(object obj)
        {
            if (obj == null)
                return false;
            if (GetType() != obj.GetType()) return false;

            return Equals((Presence)obj);
        }

        bool Equals(Presence other)
        {
            return Status == other.Status && string.Equals(Message, other.Message);
        }

        /// <summary>
        /// Get the hashcode for this object
        /// </summary>
        /// <returns>the hashcode</returns>
        public override int GetHashCode()
        {
            unchecked
            {
                return ((int) Status * 397) ^ (Message?.GetHashCode() ?? 0);
            }
        }
    }
}
