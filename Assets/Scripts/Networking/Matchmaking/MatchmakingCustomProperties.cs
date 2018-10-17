using System;

namespace UnityEngine.Ucg.Matchmaking
{
    /// <summary>
    /// This is an example of custom player properties
    /// </summary>
    [Serializable]
    public class MatchmakingPlayerProperties
    {
        [SerializeField]
        public int hats;
    }

    /// <summary>
    /// This is an example of custom match request properties for a group of players
    /// </summary>
    [Serializable]
    public class MatchmakingGroupProperties
    {
        [SerializeField]
        public int mode;
    }
}