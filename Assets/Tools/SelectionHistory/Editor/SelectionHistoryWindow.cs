using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System;
using System.Linq;

public class SelectionHistoryWindow : EditorWindow
{
    [MenuItem("FPS Sample/Windows/Selection History")]
    public static void ShowWindow()
    {
        GetWindow<SelectionHistoryWindow>(false, "Selection History", true);
    }

    
    const int k_HistorySize = 20;
    const float k_LineHeight = 20.0f;
    
    [Serializable]
    public class HistoryEntry
    {
        public HistoryEntry(UnityEngine.Object o)
        {
            obj = o;
            faviorite = false;
        }
        public UnityEngine.Object obj;
        public bool faviorite;
    }

    public List<HistoryEntry> m_History;

    private bool updateScrollPos;
    GUIStyle historyItemStyle;

    public SelectionHistoryWindow()
    {
        m_History = new List<HistoryEntry>();
    }

    private void OnSelectionChange()
    {

        if (!Selection.activeObject)
            return;
        var obj = Selection.activeObject;
    

        updateScrollPos = true;
        Repaint();
        
        for (var i = 0; i < m_History.Count; i++)
        {
            if (m_History[i].obj == obj)
            {

                return;
            }
        }
    

        m_History.Insert(0,new HistoryEntry(obj));

        // Nuke oldest non-favourite if we are above max count
        if(m_History.Count > k_HistorySize)
        {
            for(var i = m_History.Count-1; i>0; i--)
            {
                if (!m_History[i].faviorite)
                {
                    m_History.RemoveAt(i);
                    break;
                }
            }
        }
    }


    public static GUIStyle CreateObjectReferenceStyle()
    {
        var style = new GUIStyle();
        style.contentOffset = new Vector2(4, 0);
        style.fixedHeight = k_LineHeight;
        style.stretchWidth = true;
        return style;
    } 

    public static void DrawObjectReference(UnityEngine.Object o, GUIStyle style)
    {
        // Read mouse events
        var cev = Event.current;
        var mousePos = Vector2.zero;
        var mouseLeftClick = false;
        var mouseRightClick = false;
        var mouseStartDrag = false;
        if (cev != null)
        {
            mousePos = cev.mousePosition;
            mouseLeftClick = (cev.type == EventType.MouseUp) && cev.button == 0 && cev.clickCount == 1;
            mouseRightClick = (cev.type == EventType.MouseUp) && cev.button == 1 && cev.clickCount == 1;
            mouseStartDrag = (cev.type == EventType.MouseDrag) && cev.button == 0;
        }
        
        var guiElement = new GUIContent("impossible!");
        var thumbNail = AssetPreview.GetMiniThumbnail(o);
        guiElement.image = thumbNail;
        guiElement.text = o != null ? o.name : "<invalid>";

        var lineRect = EditorGUILayout.BeginHorizontal();
        var baseCol = o != null && EditorUtility.IsPersistent(o) ? new Color(0.5f, 1.0f, 0.5f) : Color.white;

        var selected = Selection.objects.Contains(o);
        style.normal.textColor = selected ? baseCol : baseCol * 0.75f;
        GUILayout.Label(guiElement, style);
            
        EditorGUILayout.EndHorizontal();

        if (o == null)
            return;

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
            else if (mouseRightClick)
            {
                EditorGUIUtility.PingObject(o);
                Event.current.Use();
            }
            else if (mouseLeftClick)
            {
                if (Event.current.control)
                {
                    var list = new List<UnityEngine.Object>(Selection.objects);
                    if (list.Contains(o))
                        list.Remove(o);
                    else
                        list.Add(o);
                    Selection.objects = list.ToArray();
                }
                else
                    Selection.activeObject = o;
                Event.current.Use();
            }
        }


    }

    void OnGUI()
    {
        // Create gui style if needed (has to happen here as forbidden to do in constructor)
        if (historyItemStyle == null)
        {
            historyItemStyle = CreateObjectReferenceStyle();
        }
        
        m_History.RemoveAll(x => x.obj == null);

        
        var size = EditorGUILayout.BeginHorizontal();

       

        var viewHeight = size.height;
        if (updateScrollPos && Selection.activeObject != null && viewHeight > 0)
        {
            var selectedYPos = GetYPosition(Selection.activeObject);
            m_ScrollPos.y = Mathf.Clamp(m_ScrollPos.y, selectedYPos - viewHeight + k_HistorySize*2, selectedYPos);
            updateScrollPos = false;
        }
      
            
        
        m_ScrollPos = GUILayout.BeginScrollView(m_ScrollPos);
        GUILayout.BeginVertical();

        DrawHistory(true);
        DrawHistory(false);
     
        GUILayout.EndVertical();
        GUILayout.EndScrollView();
        
        EditorGUILayout.EndHorizontal();

        
    }

    void DrawHistory(bool favorites)
    {
        for (var i = 0; i < m_History.Count; i++)
        {
            if (m_History[i].faviorite != favorites)
                continue;
            
            var o = m_History[i].obj;
            
            EditorGUILayout.BeginHorizontal();
            
            var newFavorit = GUILayout.Toggle(m_History[i].faviorite, "", GUILayout.Width(16.0f));
            if(newFavorit != m_History[i].faviorite)
                Repaint();
            m_History[i].faviorite = newFavorit;
            
            DrawObjectReference(o, historyItemStyle);
           
            EditorGUILayout.EndHorizontal();
        }
    }

    int GetFavoriteCount()
    {
        var count = 0;
        for (var i = 0; i < m_History.Count; i++)
        {
            if (m_History[i].faviorite)
                count++;
        }

        return count;
    }

    float GetYPosition(UnityEngine.Object obj)
    {
        var favoriteCount = GetFavoriteCount();
        var favIndex = 0;
        var nonFavIndex = 0;
        for (var i = 0; i < m_History.Count; i++)
        {
            var favorite = m_History[i].faviorite;
            
            if (m_History[i].obj == obj)
            {
                var index = favorite ? favIndex : favoriteCount + nonFavIndex;
                return k_LineHeight * index + 1;
            }
            
            if (favorite)
                favIndex++;
            else
                nonFavIndex++;
        }

        return 0;
    }
    
    Vector2 m_ScrollPos;
}
