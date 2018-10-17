using UnityEngine;
using UnityEngine.UI;

public class ChatLine : MonoBehaviour
{
    public float changeTime;
    public Text lineContent;

    public void SetText(string text)
    {
        lineContent.text = text;
        Show();
    }

    public void Show()
    {
        changeTime = Time.time;
        //lineContent.CrossFadeAlpha(1.0f, 0.1f, false);
        lineContent.enabled = true; // TODO (petera) remove once fade bug is fixed
    }

    public void Hide()
    {
        //lineContent.CrossFadeAlpha(0.0f, 0.3f, false);
        lineContent.enabled = false; // TODO (petera) remove once fade bug is fixed
    }
}
