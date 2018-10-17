using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ScoreBoard : MonoBehaviour
{
    public ScoreboardUIBinding uiBinding;

    public void SetPanelActive(bool active)
    {
        gameObject.SetActive(active);
    }
}
