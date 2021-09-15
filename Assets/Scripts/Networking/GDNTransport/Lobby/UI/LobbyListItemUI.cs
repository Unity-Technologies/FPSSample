using System.Collections.Generic;
using Macrometa;
using Macrometa.Lobby;
using TMPro;
using UnityEngine;


    public class LobbyListItemUI : MonoBehaviour {
        public TMP_Text lobbyName;
        public TMP_Text matchType;
        public TMP_Text location;
        public TMP_Text participants;
        public LobbyValue lobbyValue;
        public Prelobby prelobby;
        
        public void DisplayLobbyValue(LobbyValue aLobbyValue) {
            lobbyValue = aLobbyValue;
            
            lobbyName.text = lobbyValue.DisplayName();
            matchType.text = lobbyValue.gameMode;
            location.text = lobbyValue.region.DisplayLocation();
            participants.text = "" + (lobbyValue.team0.slots.Count  + lobbyValue.team1.slots.Count  + lobbyValue.unassigned.slots.Count)
                                   + " / " + lobbyValue.maxPlayers;
            //still need other lobby list level code

        }

        public void JoinLobby() {
            prelobby.JoinLobby(lobbyValue);
        }
    }
