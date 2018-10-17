using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ServerPanel : MonoBehaviour
{
    public TextMeshProUGUI serverInfo;

    public void SetPanelActive(bool active)
    {
        gameObject.SetActive(active);
    }

    public void SetInfo(string info)
    {
        serverInfo.text = info;
    }
}
