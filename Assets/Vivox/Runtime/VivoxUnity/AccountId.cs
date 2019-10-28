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
using System.Text;
using System.Text.RegularExpressions;

namespace VivoxUnity
{
    /// <summary>
    /// The unique identifier for a player that accesses a Vivox instance
    /// </summary>
    public sealed class AccountId
    {
        public const int _accountNameMaxLength = 127;

        private readonly string _domain;
        private readonly string _name;
        private readonly string _issuer;
        private readonly string _displayname;

        /// <summary>
        /// Create an AccountId from a URI - Internal Use Only.
        /// </summary>
        /// <param name="uri">The uri of the account.</param>
        /// <param name="displayname">Optional: The display name for an account.</param>
        public AccountId(string uri, string displayname = null)
        {
            if (string.IsNullOrEmpty(uri))
                return;

            Regex regex = new Regex(@"sip:.([^.]+).([^@]+).@([^@]+)");
            var matches = regex.Matches(uri);
            if (matches == null || matches.Count != 1 || matches[0].Groups.Count != 4)
            {
                throw new ArgumentException($"{GetType().Name}: {uri} is not a valid URI");
            }
            for (var i = 0; i < 4; ++i)
            {
                if (string.IsNullOrEmpty(matches[0].Groups[i].Value))
                    throw new ArgumentException($"{GetType().Name}: {uri} is not a valid URI");
            }
            if (!IsValidName(matches[0].Groups[2].Value))
                throw new ArgumentException("Argument contains one, or more, invalid characters.", "name");
            if ($".{matches[0].Groups[1].Value}.{matches[0].Groups[2].Value}.".Length > _accountNameMaxLength)
                throw new ArgumentException(GetType().Name+": The string \".{issuer}.{name}.\" must have a length of no greater than " + _accountNameMaxLength + " characters.");
            if (!string.IsNullOrEmpty(displayname))
            {
                byte[] displaynameBytes = Encoding.UTF8.GetBytes(displayname);
                if(displaynameBytes.Length > _accountNameMaxLength)
                    throw new ArgumentException($"{GetType().Name}: The string displayname must have a length of no greater than {_accountNameMaxLength} characters.");
            }
            _issuer = matches[0].Groups[1].Value;
            _name = matches[0].Groups[2].Value;
            _domain = matches[0].Groups[3].Value;
            _displayname = displayname;
        }
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="issuer">The issuer that is responsible for authenticating this account.</param>
        /// <param name="name">The name you assigned to the player.</param>
        /// <param name="domain">The Vivox domain that provides service for this account, for example vfd.vivox.com.</param>
        /// <param name="displayname">Optional: The display name for an account.</param>
        /// <remarks>
        /// The total length of '.{issuer}.{name}.' must be less than 63 characters, chosen only from letters a-z and numbers 0-9, and the special characters: =+-_.!~()%
        /// </remarks>
        public AccountId(string issuer, string name, string domain, string displayname = null)
        {
            if (string.IsNullOrEmpty(issuer))
                throw new ArgumentNullException(nameof(issuer));
            if (string.IsNullOrEmpty(name))
                throw new ArgumentNullException(nameof(name));
            if (string.IsNullOrEmpty(domain))
                throw new ArgumentNullException(nameof(domain));
            if (domain.IndexOf(".", StringComparison.Ordinal) <= 0)
                throw new ArgumentException($"{GetType().Name}: Argument does not look like a domain. It should be in the form like vd1.vivox.com (currently missing '.')", nameof(domain));
            if (!IsValidName(name))
                throw new ArgumentException($"{GetType().Name}: Argument contains one, or more, invalid characters.", nameof(name));
            if ($".{issuer}.{name}.".Length > _accountNameMaxLength)
                throw new ArgumentException(GetType().Name + ": The string \".{issuer }.{name}.\" must have a length of no greater than " + _accountNameMaxLength + " characters.");
            if (!string.IsNullOrEmpty(displayname))
            {
                byte[] displaynameBytes = Encoding.UTF8.GetBytes(displayname);
                if (displaynameBytes.Length > _accountNameMaxLength)
                    throw new ArgumentException($"{GetType().Name}: The string displayname must have a length of no greater than {_accountNameMaxLength} characters.");
            }

            _issuer = issuer;
            _name = name;
            _domain = domain;
            _displayname = displayname;
        }
        /// <summary>
        /// The issuer assigned to your game.
        /// </summary>
        public string Issuer => _issuer;
        /// <summary>
        /// The name you assigned to the player.
        /// </summary>
        public string Name => _name;
        /// <summary>
        /// The domain assigned to your game.
        /// </summary>
        public string Domain => _domain;

        internal string AccountName
        {
            get
            {
                if (IsEmpty) return "";
                return $".{_issuer}.{_name}.";
            }
        }
        /// <summary>
        /// The name you assigned to the player for dsiplay purposes. If one is not set then Name will be returned.
        /// </summary>
        public string DisplayName
        {
            get
            {
                // If display name is not set we will return name
                return string.IsNullOrEmpty(_displayname) ? Name : _displayname;
            }
        }
        /// <summary>
        /// Determines if two objects are equal.
        /// </summary>
        /// <param name="obj">the other object.</param>
        /// <returns>true of the objects are of equal value.</returns>
        public override bool Equals(object obj)
        {
            if (obj == null)
                return false;
            if (GetType() != obj.GetType()) return false;

            return Equals((AccountId)obj);
        }
        private bool Equals(AccountId other)
        {
            return string.Equals(_domain, other._domain) && string.Equals(_name, other._name) &&
                string.Equals(_issuer, other._issuer);
        }
        /// <summary>
        /// Hash Function for AccountId.
        /// </summary>
        /// <returns>A hash code for the current object.</returns>
        public override int GetHashCode()
        {
            unchecked
            {
                var hc = (_domain?.GetHashCode() ?? 0);
                hc *= 397;
                hc ^= (_name?.GetHashCode() ?? 0);
                hc *= 397;
                hc ^= (_issuer?.GetHashCode() ?? 0);
                return hc;
            }
        }

        /// <summary>
        /// This is true if the Name, Domain, and Issuer are empty.
        /// </summary>
        public bool IsEmpty => string.IsNullOrEmpty(_name) && string.IsNullOrEmpty(_domain) && string.IsNullOrEmpty(_issuer);

        /// <summary>
        /// Test for an empty AccountId.
        /// </summary>
        /// <param name="id">The account ID.</param>
        /// <returns>true if id is null or empty.</returns>
        public static bool IsNullOrEmpty(AccountId id)
        {
            return id == null || id.IsEmpty;
        }
        /// <summary>
        /// The network representation of the account id.
        /// </summary>
        /// <returns>A URI for this account</returns>
        /// <remarks>
        /// Note: This will be refactored in the future such that the internal network representation of the AccountId is hidden.
        /// </remarks>
        public override string ToString()
        {
            if (IsEmpty) return "";
            return $"sip:.{_issuer}.{_name}.@{_domain}";
        }
        /// <summary>
        /// Checks each character within the "name" argument against a collection of valid characters that coincide with our naming conventions.
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
