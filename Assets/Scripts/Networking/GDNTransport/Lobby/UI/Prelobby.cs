using System.Collections;
using System.Collections.Generic;
using Macrometa;
using Macrometa.Lobby;
using UnityEngine;

public class Prelobby : MonoBehaviour {

    public GameObject lobbyListWait;
    public GameObject createLobbyWait;
    public LobbyListUI lobbyListUi;
    
    public GDNClientLobbyNetworkDriver2 gdnClientLobbyNetworkDriver2;

    public void Wait(bool val) {
        lobbyListWait.SetActive(val);
        createLobbyWait.SetActive(val);
    }

    public void JoinLobby(LobbyValue lobbyValue) {
        Wait(true);
        gdnClientLobbyNetworkDriver2.JoinLobby(lobbyValue, false);
    }
    
}
