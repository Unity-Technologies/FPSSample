using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using Macrometa;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LobbyChatDisplay: MonoBehaviour {
    public string localId = "GrantAdmin";
 
    [Space]
    public TMP_Text textDisplay;
    public TMP_InputField inputField;
    public ScrollRect scrollRect;
    
    public GDNClientLobbyNetworkDriver2 gdnClientLobbyNetworkDriver2;
    public LobbyUI lobbyUi;
    
    /// <summary>
    ///  should wait for gdnStreamDriver.chatConsumerExists
    /// </summary>
    public void Start() {
        //gdnClientLobbyNetworkDriver2.localId = localId;
        inputField.Select();
        inputField.ActivateInputField(); 
    }

    public void Update() {
        while (gdnClientLobbyNetworkDriver2.gdnStreamDriver.chatMessages.Count > 0) {
            AddText(gdnClientLobbyNetworkDriver2.gdnStreamDriver.chatMessages.Dequeue());
        }

        if (gdnClientLobbyNetworkDriver2.lobbyValue != null && gdnClientLobbyNetworkDriver2.lobbyUpdateAvail) {
            GameDebug.Log("UpdateLocalLobby in testlobby transport 2");
            lobbyUi.DisplayLobbyValue(gdnClientLobbyNetworkDriver2.lobbyValue,gdnClientLobbyNetworkDriver2.clientId);
            gdnClientLobbyNetworkDriver2.lobbyUpdateAvail = false;
        }
    }
    
    #region Chat
    public void AddText(string aString) {
        textDisplay.text +=  aString +"\n";
        textDisplay.text = LimitLines(textDisplay.text);
        Canvas.ForceUpdateCanvases();
        scrollRect.verticalNormalizedPosition = 0f;
    }

    public string LimitLines(string aString, int lines = 50) {
        var stringArray =  aString.Split('\n');
        if (stringArray.Length < lines) {
            return aString;
        }
        else {
            var sb = new StringBuilder();
            for (int i = 1; i < stringArray.Length; i++) {
                sb.Append(stringArray[i] + "\n");
            }
            return sb.ToString();
        }
    }
    
    public void InputText() {
        Debug.Log( "Sent text: A " + inputField.text);
        if (inputField.text == "") return;
        Debug.Log( "Sent text: B " + inputField.text);
        SendText("<b>" + localId + "</b>: "+inputField.text);
        Debug.Log( "Sent text: B " + inputField.text);
        inputField.text = "";
        inputField.Select();
        inputField.ActivateInputField();
    }

    public void SendText(string msg ) {
        gdnClientLobbyNetworkDriver2.gdnStreamDriver.ChatSend(gdnClientLobbyNetworkDriver2.gdnStreamDriver.chatChannelId ,msg);
    }

    public void ChangeChannelId(string newChannelId) {
        
        textDisplay.text = "";
        var oldMessages = GDNStreamDriver.ChatBuffer.Dump();
        int i = 1;
        foreach (var rm in oldMessages) {
            if (rm.properties.d == newChannelId) {
                AddText(Encoding.UTF8.GetString(Convert.FromBase64String(rm.payload)));
                Debug.Log(Encoding.UTF8.GetString(Convert.FromBase64String(rm.payload))+ " : "+ i++);
            }
        }
        gdnClientLobbyNetworkDriver2.gdnStreamDriver.chatChannelId = newChannelId;
    }
    
    #endregion Chat
    
    #region Lobby
   
    #endregion Lobby
}
