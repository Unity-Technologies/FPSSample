using System.Collections;
using System.Collections.Generic;
using Macrometa;
using Macrometa.Lobby;
using TMPro;
using UnityEngine;

public class TeamUI : MonoBehaviour {
   public LobbyUI lobby;
   public int teamIndex;
   public List<PlayerUI> players;
   public TMP_InputField teamName;
   public GameObject highlight;
   
   
   private void Awake() {
      players.Clear();
      players.AddRange(GetComponentsInChildren<PlayerUI>());
   }

   // Macrometa.Lobby. is needed to stop FPSSample conflicts
   public void DisplayTeam(Macrometa.Lobby.Team team, string anOwnerId, string rttTarget, string startServer) {
      if (teamName != null) {
         teamName.text = team.name;
      }

      var ServerButtons = lobby.isAdmin;
      highlight.SetActive(false);
      var pos = team.Find(anOwnerId);
      var rttPos = team.Find(rttTarget);
      var startServerPos = team.Find(startServer);
      //Debug.Log("startServer: "+startServer + " : "+startServerPos);
      for (int i = 0; i < players.Count; i++) {
         bool highlight = pos == i;
         if (i < team.slots.Count) {
            players[i].rttTargetButton.gameObject.SetActive(ServerButtons);
            players[i].serverAllowed.gameObject.SetActive(ServerButtons);
            team.slots[i].rttTarget = (rttPos == i);
            team.slots[i].runGameServer = (startServerPos == i &&startServerPos == pos );
            players[i].DisplayPlayer(team.slots[i],highlight);
         }
         else {
            players[i].DisplayPlayer(null,false);
         }
      }
   }
   
   public void TeamSelectedClicked() {
      Debug.Log("TeamSelectedClicked(): " + teamIndex);
      lobby.TeamSelected( teamIndex);
      highlight.SetActive(true);
   }
   
   public void TeamNameChanged() {
      Debug.Log("TeamNameChanged(): " + teamIndex + " : "+teamName.text );
      lobby.TeamNameChanged(teamName.text, teamIndex);
   }
}
