using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System;

public class EditorHistoryWindow : EditorWindow
{
    const int k_HistorySize = 15;
    const float k_LineHeight = 20.0f;

    class HistoryEntry
    {
        public HistoryEntry(UnityEngine.Object o)
        {
            obj = o;
            faviourite = false;
        }
        public UnityEngine.Object obj;
        public bool faviourite;
    }

    List<HistoryEntry> m_History;

    GUIStyle historyItemStyle;

    public EditorHistoryWindow()
    {
        m_History = new List<HistoryEntry>();
        UnityEditor.Selection.selectionChanged -= OnSelectionChanged;
        UnityEditor.Selection.selectionChanged += OnSelectionChanged;
    }

    private void OnSelectionChanged()
    {
        if (!Selection.activeObject)
            return;
        var obj = Selection.activeObject;

        // If already selected, just scroll to it
        for (var i = 0; i < m_History.Count; i++)
        {
            if (m_History[i].obj == obj)
            {
                m_ScrollPos.y = k_LineHeight * i;
                Repaint();
                return;
            }
        }

        m_History.Add(new HistoryEntry(obj));

        // Nuke oldest non-favourite if we are above max count
        if(m_History.Count > k_HistorySize)
        {
            for(var i = 0; i<m_History.Count-1; i++)
            {
                if (!m_History[i].faviourite)
                {
                    m_History.RemoveAt(i);
                    break;
                }
            }
        }

        m_ScrollPos.y = k_LineHeight * m_History.Count;
        Repaint();
    }

    [MenuItem("FPS Sample/Windows/Selection History")]
    public static void ShowWindow()
    {
        GetWindow<EditorHistoryWindow>(false, "Selection History", true);
    }

    void OnGUI()
    {
        // Create gui style if needed (has to happen here as forbidden to do in constructor)
        if (historyItemStyle == null)
        {
            historyItemStyle = new GUIStyle();
            historyItemStyle.contentOffset = new Vector2(4, 0);
            historyItemStyle.fixedHeight = k_LineHeight;
            historyItemStyle.stretchWidth = true;
        }

        // Read mouse events
        var cev = Event.current;
        Vector2 mousePos = Vector2.zero;
        bool mouseClick = false;
        bool mouseDoubleClick = false;
        bool mouseStartDrag = false;
        if (cev != null)
        {
            mousePos = cev.mousePosition;
            mouseClick = (cev.type == EventType.MouseUp) && cev.button == 0 && cev.clickCount == 1;
            mouseDoubleClick = (cev.type == EventType.MouseDown) && cev.button == 0 && cev.clickCount == 2;
            mouseStartDrag = (cev.type == EventType.MouseDrag) && cev.button == 0;
        }

        m_ScrollPos = GUILayout.BeginScrollView(m_ScrollPos);
        GUILayout.BeginVertical();

        m_History.RemoveAll(x => x.obj == null);

        for (var i = 0; i < m_History.Count; i++)
        {
            var o = m_History[i].obj;
            var guiElement = new GUIContent("impossible!");
            var thumbNail = AssetPreview.GetMiniThumbnail(o);
            guiElement.image = thumbNail;
            guiElement.text = o.name;


            var lineRect = EditorGUILayout.BeginHorizontal();
            var baseCol = o != null && EditorUtility.IsPersistent(o) ? new Color(0.5f, 1.0f, 0.5f) : Color.white;
            historyItemStyle.normal.textColor = (Selection.activeObject == o) ? baseCol : baseCol * 0.75f;
            m_History[i].faviourite = GUILayout.Toggle(m_History[i].faviourite, "", GUILayout.Width(16.0f));
            GUILayout.Label(guiElement, historyItemStyle);
            EditorGUILayout.EndHorizontal();

            // Handle mouse clicks and drags
            if (lineRect.Contains(mousePos))
            {
                if (mouseStartDrag)
                {
                    DragAndDrop.PrepareStartDrag();
                    DragAndDrop.StartDrag(o.name);
                    DragAndDrop.objectReferences = new UnityEngine.Object[] { o };
                    Event.current.Use();
                }
                else if (mouseClick)
                {
                    EditorGUIUtility.PingObject(o);
                    Event.current.Use();
                }
                else if (mouseDoubleClick)
                {
                    Selection.activeObject = o;
                    Event.current.Use();
                }
            }
        }
        GUILayout.EndVertical();
        GUILayout.EndScrollView();
    }

    Vector2 m_ScrollPos;
}
