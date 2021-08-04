using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class ServerListEntry : MonoBehaviour
{
    public TMPro.TextMeshProUGUI serverName;
    [FormerlySerializedAs("hostName")] public TMPro.TextMeshProUGUI status;
    public TMPro.TextMeshProUGUI gameMode;
    public TMPro.TextMeshProUGUI mapName;
    public TMPro.TextMeshProUGUI numPlayers;
    public TMPro.TextMeshProUGUI pingTime;
}
