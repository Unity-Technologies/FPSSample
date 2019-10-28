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
using System.Text.RegularExpressions;

namespace VivoxUnity
{
    /// <summary>
    /// The unique identifier for a channel. Channels are created and destroyed automatically on demand.
    /// </summary>
    public sealed class ChannelId
    {
        internal string GetUriDesignator(ChannelType value)
        {
            switch (value)
            {
                case ChannelType.Echo:
                    return "e";
                case ChannelType.NonPositional:
                    return "g";
                case ChannelType.Positional:
                    return "d";
            }
            throw new ArgumentException($"{GetType().Name}: {value} has no GetUriDesignator() support");
        }

        private readonly string _domain;
        private readonly string _name;
        private readonly string _issuer;
        private readonly ChannelType _type;
        private readonly Channel3DProperties _properties;

        public ChannelId(string uri)
        {
            if (string.IsNullOrEmpty(uri))
                return;
            Regex regex = new Regex(@"sip:confctl-(e|g|d)-([^.]+).([^!|@]+)(?:!p-([^@]+))?@([^@]+)");
            var matches = regex.Matches(uri);
            if (matches == null || matches.Count != 1 || matches[0].Groups.Count != 6)
            {
                throw new ArgumentException($"'{uri}' is not a valid URI");
            }
            var type = matches[0].Groups[1].Value;
            switch (type)
            {
                case "g":
                    _type = ChannelType.NonPositional;
                    break;
                case "e":
                    _type = ChannelType.Echo;
                    break;
                case "d":
                    _type = ChannelType.Positional;
                    break;
                default:
                    throw new ArgumentException($"{GetType().Name}: {uri} is not a valid URI");
            }
            _issuer = matches[0].Groups[2].Value;
            _name = matches[0].Groups[3].Value;
            if (_type == ChannelType.Positional)
            {
                _properties = new Channel3DProperties(matches[0].Groups[4].Value);
            }
            _domain = matches[0].Groups[5].Value;
        }

        /// <summary>
        /// Constructor for creating an echo or non-positional channel.
        /// </summary>
        /// <param name="issuer">The issuer that is responsible for authorizing access to this channel</param>
        /// <param name="name">The name of the channel</param>
        /// <param name="domain">The Vivox domain that hosts this channel</param>
        /// <param name="type">The channel type</param>
        /// /// <param name="properties">3D positional channel properties.</param>
        public ChannelId(string issuer, string name, string domain, ChannelType type = ChannelType.NonPositional, Channel3DProperties properties = null)
        {
            if (string.IsNullOrEmpty(issuer)) throw new ArgumentNullException(nameof(issuer));
            if (string.IsNullOrEmpty(name)) throw new ArgumentNullException(nameof(name));
            if (string.IsNullOrEmpty(domain)) throw new ArgumentNullException(nameof(domain));
            if (!Enum.IsDefined(typeof(ChannelType), type)) throw new ArgumentOutOfRangeException(type.ToString());
            if (properties == null) _properties = new Channel3DProperties();
            else if (type == ChannelType.Positional && !properties.IsValid()) throw new ArgumentException("", nameof(properties));
            else _properties = properties;
            if (!IsValidName(name)) throw new ArgumentException($"{GetType().Name}: Argument contains one, or more, invalid characters, or the length of the name exceeds 200 characters.", nameof(name));

            _issuer = issuer;
            _name = name;
            _domain = domain;
            _type = type;
        }

        /// <summary>
        /// The issuer that is responsible for authorizing access to this channel
        /// </summary>
        public string Issuer => _issuer;
        /// <summary>
        /// The name of the channel
        /// </summary>
        public string Name => _name;
        /// <summary>
        /// The Vivox domain hosting this channel
        /// </summary>
        /// <example>vfd.vivox.com for development systems, vfp.vivox.com for production systems, or as provided to you by your developer support representative.</example>
        public string Domain => _domain;
        /// <summary>
        /// The channel type
        /// </summary>
        public ChannelType Type => _type;
        /// <summary>
        /// 3D channel properties
        /// </summary>
        public Channel3DProperties Properties => _properties;

        /// <summary>
        /// Determines if two objects are equal
        /// </summary>
        /// <param name="obj">the other object</param>
        /// <returns>true of the objects are of equal value</returns>
        public override bool Equals(object obj)
        {
            if (obj == null)
                return false;
            if (GetType() != obj.GetType()) return false;

            return Equals((ChannelId)obj);
        }

        bool Equals(ChannelId other)
        {
            return string.Equals(_domain, other._domain) && string.Equals(_name, other._name) &&
                string.Equals(_issuer, other._issuer) && _type == other._type;
        }
        /// <summary>
        /// Hash Function for ChannelId
        /// </summary>
        /// <returns>A hash code for the current object</returns>
        public override int GetHashCode()
        {
            unchecked
            {
                var hc = (_domain?.GetHashCode() ?? 0);
                hc = (hc * 397) ^ (_name?.GetHashCode() ?? 0);
                hc = (hc * 397) ^ (_issuer?.GetHashCode() ?? 0);
                hc = (hc * 397) ^ (_domain?.GetHashCode() ?? 0);
                hc = (hc * 397) ^ _type.GetHashCode();
                return hc;
            }
        }

        /// <summary>
        /// This is true if the Name, Domain, and Issuer are empty.
        /// </summary>
        public bool IsEmpty => string.IsNullOrEmpty(_name) && string.IsNullOrEmpty(_domain) && string.IsNullOrEmpty(_issuer);
        /// <summary>
        /// Test for an empty ChannelId
        /// </summary>
        /// <param name="id">The channel ID</param>
        /// <returns>true if id is null or empty</returns>
        public static bool IsNullOrEmpty(ChannelId id)
        {
            return id == null || id.IsEmpty;
        }
        /// <summary>
        /// The network representation of the channel id
        /// Note: This will be refactored in the future such that the internal network representation of the ChannelId is hidden.
        /// </summary>
        /// <returns>A URI for this channel</returns>
        public override string ToString()
        {
            if (!IsValid()) return "";

            string props = _type == ChannelType.Positional ? _properties.ToString() : string.Empty;
            return $"sip:confctl-{GetUriDesignator(_type)}-{_issuer}.{_name}{props}@{_domain}";
        }
        /// <summary>
        /// Ensures that _name, _domain, and _issuer are not empty, and further validates the _name field by checking it against an array of valid characters.
        /// If the channel is Positional, _properties is also validated.
        /// </summary>
        /// <returns>If the ChannelID is valid.</returns>
        internal bool IsValid()
        {
            return !(IsEmpty
                && IsValidName(_name)
                && (_type == ChannelType.Positional && !_properties.IsValid()));
        }
        /// <summary>
        /// Checks the value of "name" against a group of valid characters.
        /// </summary>
        /// <returns>If the name is valid.</returns>
        internal bool IsValidName(string name)
        {
            const string validChars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ1234567890=+-_.!~()%";
            foreach (char c in name.ToCharArray())
            {
                if (!validChars.Contains(c.ToString()))
                {
                    return false;
                }
            }
            return true;
        }
    }
}
