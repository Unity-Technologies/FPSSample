using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using Macrometa;
using UnityEngine;
using UnityEngine.UI;

namespace Macrometa {
    public class ChatDisplay : MonoBehaviour {
        public TMPro.TextMeshProUGUI textDisplay;
        public TMPro.TMP_InputField inputField;
        public ScrollRect scrollRect;

        public GDNClientBrowserNetworkDriver gdnClientBrowserNetworkDriver;


        /// <summary>
        ///  should wait for gdnStreamDriver.chatConsumerExists
        /// </summary>
        public void Start() {
            inputField.Select();
            inputField.ActivateInputField();
        }

        public void Update() {
            while (gdnClientBrowserNetworkDriver.gdnStreamDriver.chatMessages.Count > 0) {
                AddText(gdnClientBrowserNetworkDriver.gdnStreamDriver.chatMessages.Dequeue());
            }
        }

        public void AddText(string aString) {
            textDisplay.text += aString + "\n";
            textDisplay.text = LimitLines(textDisplay.text);
            Canvas.ForceUpdateCanvases();
            scrollRect.verticalNormalizedPosition = 0f;
        }

        public string LimitLines(string aString, int lines = 50) {
            var stringArray = aString.Split('\n');
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
            if (inputField.text == "") return;
            SendText(inputField.text);
            Debug.Log("Sent text: " + inputField.text);
            inputField.text = "";
            inputField.Select();
            inputField.ActivateInputField();

        }

        public void SendText(string msg) {
            gdnClientBrowserNetworkDriver.gdnStreamDriver.ChatSend(
                gdnClientBrowserNetworkDriver.gdnStreamDriver.chatChannelId, msg);
        }

        public void ChangeChannelId(string newChannelId) {

            textDisplay.text = "";
            var oldMessages = GDNStreamDriver.ChatBuffer.Dump();
            int i = 1;
            foreach (var rm in oldMessages) {
                if (rm.properties.d == newChannelId) {
                    AddText(Encoding.UTF8.GetString(Convert.FromBase64String(rm.payload)));
                    Debug.Log(Encoding.UTF8.GetString(Convert.FromBase64String(rm.payload)) + " : " + i++);
                }
            }

            gdnClientBrowserNetworkDriver.gdnStreamDriver.chatChannelId = newChannelId;
        }
    }
}