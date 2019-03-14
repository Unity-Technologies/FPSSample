using System;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace UnityEngine.Ucg.Matchmaking
{
    class MatchmakingClient
    {
        internal string Url { get; }

        const string k_CreateRequestEndpoint = "/request";

        const string k_GetAssignmentEndpoint = "/assignment";

        const string k_ApiVersion = "1";

        internal MatchmakingClient(string endpoint)
        {
            Url = "https://" + endpoint + "/api/v" + k_ApiVersion + "/matchmaking";
        }

        /// <summary>
        /// Start matchmaking for a provided request. This tells your matchmaking endpoint to add
        /// the players and group data in the request to the matchmaking pool for consideration
        /// </summary>
        /// <param name="request">The matchmaking request</param>
        /// <returns>An asynchronous operation that can be used in various async flow patterns.
        /// The webrequest inside will contain a json success object</returns>
        /// TODO: Strongly type expect contract return from successful call
        internal UnityWebRequestAsyncOperation RequestMatchAsync(MatchmakingRequest request)
        {
            string url = Url + k_CreateRequestEndpoint;
            UnityWebRequest webRequest = new UnityWebRequest(url, "POST");
            webRequest.SetRequestHeader("Content-Type", "application/json");
            string txtRec = JsonUtility.ToJson(request);
            byte[] jsonToSend = new UTF8Encoding().GetBytes(txtRec);
            webRequest.uploadHandler = new UploadHandlerRaw(jsonToSend);
            webRequest.downloadHandler = new DownloadHandlerBuffer();
            Debug.Log("Calling... " + url + " " + txtRec);
            return webRequest.SendWebRequest();
        }

        /// <summary>
        /// Retrieve the assignment for a given player. This call will perform a long GET while listening for
        /// matchmaking results
        /// </summary>
        /// <param name="id">The id of a player</param>
        /// <returns>An asynchronous operation that can be used in various async flow patterns.
        /// The webrequest inside will contain a json connection string object</returns>
        /// TODO: Strongly type expect contract return from successful call
        internal UnityWebRequestAsyncOperation GetAssignmentAsync(string id)
        {
            string url = Url + k_GetAssignmentEndpoint + "?id=" + id;
            UnityWebRequest webRequest = new UnityWebRequest(url, "GET");
            webRequest.SetRequestHeader("Content-Type", "application/json");
            webRequest.downloadHandler = new DownloadHandlerBuffer();
            Debug.Log("Calling... " + url);
            return webRequest.SendWebRequest();
        }
    }
}
