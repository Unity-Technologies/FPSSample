using System;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Text.RegularExpressions;

public class ChatPanel : MonoBehaviour
{
    public bool isOpen { get { return m_IsOpen; } }

    public const int maxLines = 20;

    static string[] messagePrefixes = new string[] { "/all ", "/team " };

    string defaultPrefix = "/team ";

    public GameObject content;
    public ChatLine lineTemplate;
    public ScrollRect scrollRect;
    public TMPro.TMP_InputField field;
    public Image[] backgroundImages;

    [NonSerialized] public KeyCode activationKey = KeyCode.Return;

    void Awake()
    {
        field.interactable = false;
        field.text = defaultPrefix;
        field.textComponent.enabled = false;
        m_MoveToEnd = true;

        var submitEvent = new TMPro.TMP_InputField.SubmitEvent();
        submitEvent.AddListener(OnEndEdit);
        field.onEndEdit = submitEvent;

        FadeBackgrounds(0.0f, 0.1f);
    }

    internal void SetPanelActive(bool active)
    {
        gameObject.SetActive(active);
    }

    public void Tick(ChatSystemClient chatSystem)
    {
        // Handle outgoing messages
        foreach (var l in m_ChatLinesToSend)
        {
            if (chatSystem != null)
                chatSystem.SendMessage(l);
            else
                AddLine(l);
        }
        m_ChatLinesToSend.Clear();

        // Handle incoming messages
        if (chatSystem != null)
        {
            while (chatSystem.incomingMessages.Count > 0)
            {
                var message = chatSystem.incomingMessages.Dequeue();
                AddLine(message);
            }
        }

        if (m_MoveToEnd)
        {
            field.MoveTextEnd(true);
            m_MoveToEnd = false;
        }
        if (!m_IsOpen && Game.Input.GetKeyDown(activationKey))
        {
            field.textComponent.enabled = true;
            field.interactable = true;

            field.ActivateInputField();
            m_MoveToEnd = true;

            m_IsOpen = true;
            Game.Input.SetBlock(Game.Input.Blocker.Chat, true);

            FadeBackgrounds(1.0f, 0.2f);

            foreach (var line in m_Lines)
                line.Show();
        }
        else if (m_IsOpen && !field.isFocused)
        {
            m_IsOpen = false;
            Game.Input.SetBlock(Game.Input.Blocker.Chat, false);
            field.interactable = false;

            FadeBackgrounds(0.0f, 0.7f);

            foreach (var line in m_Lines)
                line.changeTime = Time.time;
        }
        else if (m_IsOpen && Input.GetKeyDown(KeyCode.Tab))
        {
            var text = field.text;
            for (int i = 0, l = messagePrefixes.Length; i < l; ++i)
            {
                var prefixMatch = text.PrefixMatch(messagePrefixes[i]);
                if (prefixMatch > 1)
                {
                    var oldCaretReverse = field.text.Length - field.caretPosition;
                    defaultPrefix = messagePrefixes[(i + 1) % l];
                    field.text = defaultPrefix + text.Substring(prefixMatch);
                    field.caretPosition = field.text.Length - oldCaretReverse;
                    break;
                }
            }
        }

        if (!m_IsOpen)
        {
            // Fade out old lines
            foreach (var line in m_Lines)
            {
                if (Time.time - line.changeTime > 4.0f)
                    line.Hide();
            }
            field.textComponent.enabled = false;
        }

    }

    public void ClearMessages()
    {
        // Fade out all message lines
        foreach (var l in m_Lines)
            l.Hide();
    }

    Regex m_EmptyMessageRegex = new Regex(@"^/(\w+)\s+$"); // match 'empty' messages like e.g. "/all "
    void OnEndEdit(string value)
    {
        if (!Input.GetKey(KeyCode.Return) && !Input.GetKey(KeyCode.KeypadEnter))
            return;

        field.DeactivateInputField();

        field.text = defaultPrefix;
        m_MoveToEnd = true;
        if (string.IsNullOrEmpty(value) || m_EmptyMessageRegex.IsMatch(value))
            return;

        m_ChatLinesToSend.Add(value);
    }

    void AddLine(string line)
    {
        //GameDebug.Log("Chat: " + line);
        ChatLine chatLine;
        if (content.transform.childCount <= maxLines)
        {
            chatLine = Instantiate<ChatLine>(lineTemplate, content.transform);
            chatLine.gameObject.SetActive(true);
            m_Lines.Add(chatLine);
        }
        else
        {
            chatLine = content.transform.GetChild(1).GetComponent<ChatLine>();
            chatLine.transform.SetSiblingIndex(content.transform.childCount);
        }
        chatLine.SetText(line);

        Canvas.ForceUpdateCanvases(); // TODO (petera) why is this needed?

        var contentRect = ((RectTransform)content.transform).rect;
        var panelRect = ((RectTransform)transform).rect;

        var viewTransform = (RectTransform)scrollRect.transform;
        var size = viewTransform.sizeDelta;
        size.y = contentRect.height < panelRect.height ? contentRect.height : panelRect.height;
        viewTransform.sizeDelta = size;

        scrollRect.verticalNormalizedPosition = 0f;
    }

    void FadeBackgrounds(float alpha, float duration)
    {
        foreach (var image in backgroundImages)
        {
            image.enabled = alpha > 0.0f;
            //image.CrossFadeAlpha(alpha, duration, false);
        }
    }

    bool m_IsOpen;
    bool m_MoveToEnd;
    List<ChatLine> m_Lines = new List<ChatLine>();
    List<string> m_ChatLinesToSend = new List<string>();
}
