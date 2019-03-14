using System;
using UnityEngine;
using UnityEngine.Networking;

namespace UnityEngine.Ucg.Matchmaking
{
    class MatchmakingController
    {
        public delegate void RequestMatchSuccess();
        public delegate void RequestMatchError(string error);
        public delegate void GetAssignmentSuccess(Assignment assignment);
        public delegate void GetAssignmentError(string error);

        RequestMatchSuccess m_RequestMatchSuccess;
        RequestMatchError m_RequestMatchError;
        GetAssignmentSuccess m_GetAssignmentSuccess;
        GetAssignmentError m_GetAssignmentError;

        MatchmakingClient m_Client;

        UnityWebRequestAsyncOperation m_RequestMatchOperation;

        UnityWebRequestAsyncOperation m_GetAssignmentOperation;

        internal MatchmakingController(string endpoint)
        {
            m_Client = new MatchmakingClient(endpoint);
        }

        /// <summary>
        /// Start a matchmaking request call on the controller
        /// </summary>
        internal void StartRequestMatch(MatchmakingRequest request, RequestMatchSuccess successCallback, RequestMatchError errorCallback)
        {
            m_RequestMatchOperation = m_Client.RequestMatchAsync(request);
            m_RequestMatchSuccess = successCallback;
            m_RequestMatchError = errorCallback;
        }

        /// <summary>
        /// Update the state of the request. If it is complete, this will invoke the correct registered callback
        /// </summary>
        internal void UpdateRequestMatch()
        {
            if (m_RequestMatchOperation == null)
            {
                Debug.Log("You must call StartRequestMatch first");
                return;
            }
            else if (!m_RequestMatchOperation.isDone)
            {
                return;
            }
            
            if (m_RequestMatchOperation.webRequest.isNetworkError || m_RequestMatchOperation.webRequest.isHttpError)
            {
                Debug.LogError("There was an error calling matchmaking RequestMatch. Error: " + m_RequestMatchOperation.webRequest.error);
                m_RequestMatchError.Invoke(m_RequestMatchOperation.webRequest.error);
                return;
            }

            MatchmakingResult result = JsonUtility.FromJson<MatchmakingResult>(m_RequestMatchOperation.webRequest.downloadHandler.text);
            if (!result.success)
            {
                m_RequestMatchError.Invoke(result.error);
                return;
            }

            m_RequestMatchSuccess.Invoke();
        }

        /// <summary>
        /// Start a matchmaking request to get the provided player's assigned connection information
        /// </summary>
        internal void StartGetAssignment(string id, GetAssignmentSuccess successCallback, GetAssignmentError errorCallback)
        {
            m_GetAssignmentOperation = m_Client.GetAssignmentAsync(id);
            m_GetAssignmentSuccess = successCallback;
            m_GetAssignmentError = errorCallback;
        }

        /// <summary>
        /// Update the state of the request. If it is complete, this will invoke the correct registered callback
        /// </summary>
        internal void UpdateGetAssignment()
        {
            if (m_GetAssignmentOperation == null)
            {
                Debug.Log("You must call StartGetAssignment first");
                return;
            }
            else if (!m_GetAssignmentOperation.isDone)
            {
                return;
            }

            if (m_GetAssignmentOperation.webRequest.isNetworkError || m_GetAssignmentOperation.webRequest.isHttpError)
            {
                Debug.LogError("There was an error calling matchmaking getAssignment. Error: " + m_GetAssignmentOperation.webRequest.error);
                m_GetAssignmentError.Invoke(m_GetAssignmentOperation.webRequest.error);
                return;
            }

            Assignment result = JsonUtility.FromJson<Assignment>(m_GetAssignmentOperation.webRequest.downloadHandler.text);

            if (!string.IsNullOrEmpty(result.AssignmentError))
            {
                m_GetAssignmentError.Invoke(result.AssignmentError);
                return;
            }

            m_GetAssignmentSuccess.Invoke(result);

        }
    }
}
