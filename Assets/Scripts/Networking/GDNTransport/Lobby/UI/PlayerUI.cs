using System;
using System.Collections;
using System.Collections.Generic;
using Macrometa.Lobby;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayerUI : MonoBehaviour {
    public string clientID; // used so buttons can send back an ID
    public LobbyUI lobby;
    public GameObject emptySlot;
    public GameObject activePlayer;
    public TMP_Text playerName;
    public TMP_Text pingTime;
    public TMP_Text rttTime;
    public Button rttTargetButton; ////shown to admin to select who to ping
    public Button serverAllowed; //shown to admin to select who can start game server
    public Button startGameServer; // shown to player to start game server
    public GameObject highlight;
    public GameObject highlightRttTarget;
    public Image highlightRttTime;
    
    public void DisplayPlayer(TeamSlot teamSlot, bool isHighlight) {
        if (teamSlot == null) {
            emptySlot.SetActive(true);
            activePlayer.SetActive(false);
            highlight.SetActive(false);
            return;
        }

        highlight.SetActive(isHighlight);
        emptySlot.SetActive(false);
        activePlayer.SetActive(true);
        playerName.text = teamSlot.playerName;
        pingTime.text = teamSlot.ping.ToString();
        if (teamSlot.rtt > 0) {
            rttTime.text = teamSlot.rtt.ToString();
            highlightRttTime.gameObject.SetActive(true);
            if (teamSlot.rtt > 300) {
                highlightRttTime.color = Color.red;
            } else if (teamSlot.rtt > 200) {
                highlightRttTime.color = new Color(0.85f, 0.45f, 0);
            }
            else {
                highlightRttTime.color = Color.green;
            }
        }
        else {
            rttTime.text = "";
            highlightRttTime.gameObject.SetActive(false);
        }

        if (teamSlot.rttTarget) {
            highlightRttTarget.SetActive(true);
        }
        else {
            highlightRttTarget.SetActive(false);
        }
        if (teamSlot.runGameServer) {
            startGameServer.gameObject.SetActive(true);
        }
        else {
            startGameServer.gameObject.SetActive(false);
        }
        clientID = teamSlot.clientId;

    }
    
    public void ServerAllowedClicked() {
        GameDebug.Log("pushed ServerAllowedClicked");
        lobby.ServerAllowed(clientID);
    }
    
    public void RttTargetClicked() {
        lobby.SetRttTarget(clientID);
    }
    
}
