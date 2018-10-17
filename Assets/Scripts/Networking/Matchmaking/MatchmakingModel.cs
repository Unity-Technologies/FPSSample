using System;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEngine.Ucg.Matchmaking
{
    [Serializable]
    public class MatchmakingPlayer
    {
        [SerializeField]
        string id;

        [SerializeField]
        string properties;

        public string Id => id;

        public string Properties
        {
            get { return properties; }
            set { properties = value; }
        }

        internal MatchmakingPlayer(string id)
        {
            this.id = id;
        }
    }

    [Serializable]
    public class MatchmakingRequest
    {
        [SerializeField]
        List<MatchmakingPlayer> players;

        [SerializeField]
        string properties;

        public List<MatchmakingPlayer> Players
        {
            get { return players; }
            set { players = value; }
        }

        public string Properties
        {
            get { return properties; }
            set { properties = value; }
        }

        public MatchmakingRequest()
        {
            this.players = new List<MatchmakingPlayer>();
        }
    }

    class MatchmakingResult
    {
        [SerializeField]
        internal bool success;

        [SerializeField]
        internal string error;
    }

    [Serializable]
    class AssignmentRequest
    {
        [SerializeField]
        string id;

        public string Id => id;

        internal AssignmentRequest(string id)
        {
            this.id = id;
        }
    }

    [Serializable]
    class ConnectionInfo
    {
        [SerializeField]
        string connection_string;

        public string ConnectionString => connection_string;
    }
}
