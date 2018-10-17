using UnityEditor;
using UnityEngine;
using System.Collections.Generic;

namespace AssetBundleBrowser
{
    internal class MessageList
    {
        private Vector2 m_ScrollPosition = Vector2.zero;

        private GUIStyle[] m_Style = new GUIStyle[2];

        IEnumerable<AssetBundleModel.AssetInfo> m_Selecteditems;
        List<MessageSystem.Message> m_Messages;

        Vector2 m_Dimensions = new Vector2(0, 0);
        const float k_ScrollbarPadding = 16f;
        const float k_BorderSize = 1f;


        internal MessageList()
        {
            Init();
        }
        private void Init()
        {
            m_Style[0] = "OL EntryBackOdd";
            m_Style[1] = "OL EntryBackEven";
            m_Style[0].wordWrap = true;
            m_Style[1].wordWrap = true;
            m_Style[0].padding = new RectOffset(32, 0, 1, 4);
            m_Style[1].padding = new RectOffset(32, 0, 1, 4);
            m_Messages = new List<MessageSystem.Message>();

        }
        internal void OnGUI(Rect fullPos)
        {
            DrawOutline(fullPos, 1f);

            Rect pos = new Rect(fullPos.x + k_BorderSize, fullPos.y + k_BorderSize, fullPos.width - 2 * k_BorderSize, fullPos.height - 2 * k_BorderSize);
            

            if (m_Dimensions.y == 0 || m_Dimensions.x != pos.width - k_ScrollbarPadding)
            {
                //recalculate height.
                m_Dimensions.x = pos.width - k_ScrollbarPadding;
                m_Dimensions.y = 0;
                foreach (var message in m_Messages)
                {
                    m_Dimensions.y += m_Style[0].CalcHeight(new GUIContent(message.message), m_Dimensions.x);
                }
            }

            m_ScrollPosition = GUI.BeginScrollView(pos, m_ScrollPosition, new Rect(0, 0, m_Dimensions.x, m_Dimensions.y));
            int counter = 0;
            float runningHeight = 0.0f;
            foreach (var message in m_Messages) 
            {
                int index = counter % 2;
                var content = new GUIContent(message.message);
                float height = m_Style[index].CalcHeight(content, m_Dimensions.x);

                GUI.Box(new Rect(0, runningHeight, m_Dimensions.x, height), content, m_Style[index]);
                GUI.DrawTexture(new Rect(0, runningHeight, 32f, 32f), message.icon);
                //TODO - cleanup formatting issues and switch to HelpBox
                //EditorGUI.HelpBox(new Rect(0, runningHeight, m_dimensions.x, height), message.message, (MessageType)message.severity);

                counter++;
                runningHeight += height;
            }
            GUI.EndScrollView();
        }

        internal void SetItems(IEnumerable<AssetBundleModel.AssetInfo> items)
        {
            m_Selecteditems = items;
            CollectMessages();
        }

        internal void CollectMessages()
        {
            m_Messages.Clear();
            m_Dimensions.y = 0f;
            if(m_Selecteditems != null)
            {
                foreach (var asset in m_Selecteditems)
                {
                    m_Messages.AddRange(asset.GetMessages());
                }
            }
        }

        internal static void DrawOutline(Rect rect, float size)
        {
            Color color = new Color(0.6f, 0.6f, 0.6f, 1.333f);
            if(EditorGUIUtility.isProSkin)
            {
                color.r = 0.12f;
                color.g = 0.12f;
                color.b = 0.12f;
            }

            if (Event.current.type != EventType.Repaint)
                return;

            Color orgColor = GUI.color;
            GUI.color = GUI.color * color;
            GUI.DrawTexture(new Rect(rect.x, rect.y, rect.width, size), EditorGUIUtility.whiteTexture);
            GUI.DrawTexture(new Rect(rect.x, rect.yMax - size, rect.width, size), EditorGUIUtility.whiteTexture);
            GUI.DrawTexture(new Rect(rect.x, rect.y + 1, size, rect.height - 2 * size), EditorGUIUtility.whiteTexture);
            GUI.DrawTexture(new Rect(rect.xMax - size, rect.y + 1, size, rect.height - 2 * size), EditorGUIUtility.whiteTexture);

            GUI.color = orgColor;
        }
    }
}
