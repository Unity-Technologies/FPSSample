using System.Collections.Generic;
using Macrometa;
using Macrometa.Lobby;
using TMPro;
using UnityEngine;


    public class LobbyListUI : MonoBehaviour {
        //public List<LobbyListItemUI> items;
        public LobbyListItemUI prefabItem;
        public GameObject itemHolder;
        public Prelobby prelobby;

        public void DisplayLobbies(LobbyList lobbyList) {
            foreach (Transform child in itemHolder.transform) {
               Destroy(child.gameObject);
            }
            itemHolder.transform.DetachChildren();
            foreach (var lv in lobbyList.lobbies) {
                if (lv.closeLobbyNow) continue;
                var lli = Instantiate<LobbyListItemUI>(prefabItem);
                lli.gameObject.SetActive(true);
                lli.transform.SetParent(itemHolder.transform);
                lli.DisplayLobbyValue(lv);
            }
        }
    }
