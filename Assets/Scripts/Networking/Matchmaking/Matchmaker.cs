using System;
using System.Net;

namespace UnityEngine.Ucg.Matchmaking
{
    public class Matchmaker
    {
        /// <summary>
        /// The ip:port of the matchmaking service
        /// </summary>
        public string Endpoint;

        MatchmakingRequest MatchmakingRequest;

        MatchmakingController matchmakingController;

        public delegate void SuccessCallback(string connectionInfo);

        public delegate void ErrorCallback(string error);

        SuccessCallback m_Success;
        ErrorCallback m_Error;

        public enum MatchmakingState
        {
            None,
            Requesting,
            Searching,
            Found,
            Error
        };

        /// <summary>
        /// The matchmaking state machine's current state
        /// </summary>
        public MatchmakingState State = MatchmakingState.None;

        public Matchmaker(string endpoint)
        {
            Endpoint = endpoint;
        }

        /// <summary>
        /// Matchmaking state-machine driver
        /// </summary>
        /// <exception cref="ArgumentException"></exception>
        public void UpdateMatchmaking()
        {
            switch (State)
            {
                case MatchmakingState.Requesting:
                    matchmakingController.UpdateRequestMatch();
                    break;
                case MatchmakingState.Searching:
                    matchmakingController.UpdateGetAssignment();
                    break;
                case MatchmakingState.Found:
                case MatchmakingState.Error:
                    break; // User hasn't stopped the state machine yet.
                default:
                    throw new ArgumentException();
            }
        }

        /// <summary>
        /// Generates a matchmaking request from the custom player and group properties provided.
        /// </summary>
        /// <param name="playerId">The id of the player</param>
        /// <param name="playerProps">Custom player properties relevant to the matchmaking function</param>
        /// <param name="groupProps">Custom group properties relevant to the matchmaking function</param>
        /// <returns></returns>
        public static MatchmakingRequest CreateMatchmakingRequest(string playerId, MatchmakingPlayerProperties playerProps, MatchmakingGroupProperties groupProps)
        {
            MatchmakingRequest request = new MatchmakingRequest();
            MatchmakingPlayer thisPlayer = new MatchmakingPlayer(playerId);
            
            thisPlayer.Properties = JsonUtility.ToJson(playerProps);

            request.Players.Add(thisPlayer);
            request.Properties = JsonUtility.ToJson(groupProps);

            return request;
        }

        /// <summary>
        /// Start matchmaking
        /// </summary>
        /// <param name="request">The matchmaking request</param>
        /// <param name="successCallback">If a match is found, this callback will provide the connection information</param>
        /// <param name="errorCallback">If matchmaking fails, this callback will provided some failure information</param>
        public void RequestMatch(MatchmakingRequest request, SuccessCallback successCallback,
            ErrorCallback errorCallback)
        {
            m_Success = successCallback;
            m_Error = errorCallback;
            MatchmakingRequest = request;

            matchmakingController = new MatchmakingController(Endpoint);

            matchmakingController.StartRequestMatch(request, GetAssignment, OnError);
            State = MatchmakingState.Requesting;
            Debug.Log(State);
        }

        void GetAssignment()
        {
            matchmakingController.StartGetAssignment(MatchmakingRequest.Players[0].Id, OnSuccess, OnError);
            State = MatchmakingState.Searching;
            Debug.Log(State);
        }

        void OnSuccess(string connectionInfo)
        {
            State = MatchmakingState.Found;
            Debug.Log(State);
            m_Success.Invoke(connectionInfo);
        }

        void OnError(string error)
        {
            State = MatchmakingState.Error;
            Debug.Log(State);
            m_Error.Invoke(error);
        }
    }
}