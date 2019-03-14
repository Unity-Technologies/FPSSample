using System;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEngine.Ucg.Matchmaking
{
    [Serializable]
    public class MatchmakingPlayer
    {
#pragma warning disable 649
        [SerializeField]
        string id;

        [SerializeField]
        string properties;
#pragma warning restore 649
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
#pragma warning disable 649
        [SerializeField]
        List<MatchmakingPlayer> players;

        [SerializeField]
        string properties;
#pragma warning restore 649
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

#pragma warning disable 649
    class MatchmakingResult
    {
        [SerializeField]
        internal bool success;

        [SerializeField]
        internal string error;
    }
#pragma warning restore 649

    [Serializable]
    class AssignmentRequest
    {
#pragma warning disable 649
        [SerializeField]
        string id;
#pragma warning restore 649

        public string Id => id;

        internal AssignmentRequest(string id)
        {
            this.id = id;
        }
    }

    [Serializable]
    public class Assignment
    {
#pragma warning disable 649
        [SerializeField]
        string connection_string;

        [SerializeField]
        string assignment_error;

        [SerializeField]
        List<string> roster;
#pragma warning restore 649

        public string ConnectionString => connection_string;
        public string AssignmentError => assignment_error;
        public List<string> Roster
        {
            get { return roster; }
            set { roster = value; }
        }
    }

}
