using UnityEngine;
using UnityEngine.UI;

public class CountDown : MonoBehaviour
{
    public Text levelInfoCounter;

    public void SetPanelActive(bool active)
    {
        gameObject.SetActive(active);
    }

    public void SetCount(int count)
    {
        levelInfoCounter.Format("{0}", count);
    }
}
