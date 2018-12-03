using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;


public class MultiInspector : EditorWindow
{
    public Vector2 scrollViewPos;
    public int columnWidth = 250;
    public List<Object> objects = new List<Object>();

    Dictionary<Type,Type> customEditors = new Dictionary<Type, Type>(50);
    Dictionary<Object,Editor> editors = new Dictionary<Object, Editor>();

    [MenuItem("FPS Sample/Windows/Multi Inspector")]
    public static void ShowWindow()
    {
        GetWindow<MultiInspector>(false, "Multi Inspector", true);
    }
    
    private void OnDestroy()
    {
        foreach (var editorsValue in editors.Values)
        {
            DestroyImmediate(editorsValue);
        }
        editors.Clear();
        EditorApplication.playModeStateChanged -= EditorApplicationOnPlayModeStateChanged;
    }

    public MultiInspector()
    {
        FindAllCustomInspectors();
        
        EditorApplication.playModeStateChanged -= EditorApplicationOnPlayModeStateChanged;
        EditorApplication.playModeStateChanged += EditorApplicationOnPlayModeStateChanged;
    }

    private void EditorApplicationOnPlayModeStateChanged(PlayModeStateChange obj)
    {
        if (obj == PlayModeStateChange.ExitingEditMode)
        {
            editors.Clear();
            
            // TODO (mogensh) Remove this - but needed atm as we get some errors from custom inspectors when comming back from playmode 
            Clear();
        }
    }

    private void OnSelectionChange()
    {
        Repaint();
    }

    public void Clear()
    {
        objects.Clear();
    }
    
    public void AddSelection()
    {
        foreach (var o in Selection.objects)
        {
            if(!objects.Contains(o)) 
                objects.Add(o);
        }
        
        objects.Sort(Comparison);
        
        Repaint();
    }

    public void UseSelection()
    {
        objects.Clear();
        objects.AddRange(Selection.objects);

        objects.Sort(Comparison);
        
        foreach (var editorsValue in editors.Values)
        {
            Editor.DestroyImmediate(editorsValue);
        }
        editors.Clear();
        Repaint();
    }
    
    
    void OnGUI()
    {
        objects.RemoveAll(o => o == null);
        
        GUILayout.BeginHorizontal("Box");
        
        if (GUILayout.Button("Add selected"))
        {
            AddSelection();
        }
        
        if (GUILayout.Button("Use selected"))
        {
            UseSelection();
        }

        columnWidth = EditorGUILayout.IntField("Column width", columnWidth);
        
        GUILayout.FlexibleSpace();
        
        GUILayout.EndHorizontal();
        
        if (objects == null)
            return;
        
        scrollViewPos = GUILayout.BeginScrollView(scrollViewPos);
        
        GUILayout.BeginHorizontal();
        
        Object removedObject = null;
        foreach (var o in objects)
        {
            if (o == null)
                continue;
            
            var selected = Selection.objects.Contains(o);
            
            var skin = EditorGUIUtility.GetBuiltinSkin(EditorSkin.Inspector);
            var selectColor = skin.settings.selectionColor;
            var objectHeaderStyle = new GUIStyle( GUI.skin.box );
            
            var objectHeaderSelectedStyle = new GUIStyle( GUI.skin.box );
            objectHeaderSelectedStyle.normal.background = CreateTexture2D(2, 2, selectColor);
            
            GUILayout.BeginVertical("Box",GUILayout.MaxWidth(columnWidth),GUILayout.Width(columnWidth),GUILayout.ExpandWidth(false));
            
            // Object header
            var objectEditor = GetOrCreateEditor(o);
            
            var headerStyle = selected ? objectHeaderSelectedStyle : objectHeaderStyle;
            var headerRect = EditorGUILayout.BeginHorizontal(headerStyle);

            objectEditor.DrawHeader();
            
            if (GUILayout.Button("-"))
            {
                removedObject = o;
            }
            
            GUILayout.EndHorizontal();

            var cev = Event.current;
            if (headerRect.Contains(cev.mousePosition))
            {
                var mouseLeftClick = (cev.type == EventType.MouseUp) && cev.button == 0 && cev.clickCount == 1;
                var mouseRightClick = (cev.type == EventType.MouseUp) && cev.button == 1 && cev.clickCount == 1;
                var mouseStartDrag = (cev.type == EventType.MouseDrag) && cev.button == 0;

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
                        var list = new List<Object>(Selection.objects);
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

            
            //gameObjectEditor.DrawDefaultInspector(); 
            //gameObjectEditor.OnInspectorGUI();

            var gameObject = o as GameObject;
            if (gameObject != null)
            {
                var components = gameObject.GetComponents<Component>();
                foreach (var component in components)
                {
                    EditorGUIUtility.wideMode = true;
                    GUILayout.Label( component.GetType().Name,EditorStyles.boldLabel);

                    var componentEditor = GetOrCreateEditor(component);
                    componentEditor.OnInspectorGUI();
                    
                    EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
                }    
            }
            else
            {
                objectEditor.OnInspectorGUI();
            }

            GUILayout.EndVertical();
        }
      
        GUILayout.EndHorizontal();

        GUILayout.EndScrollView();

        if (removedObject != null)
        {
            objects.Remove(removedObject);
            editors.Remove(removedObject);
        }
    }

    private int Comparison(Object x, Object y)
    {
        var goX = x as GameObject;
        var goY = y as GameObject;

        var isXPrefInst = PrefabUtility.IsPartOfPrefabInstance(x);
        var isYPrefInst = PrefabUtility.IsPartOfPrefabInstance(y);
        if (isXPrefInst != isYPrefInst)
            return isXPrefInst ? -1 : 1;
        
        
        if(goX == null || goY == null)
            return x.GetType().Name.CompareTo(y.GetType().Name);

        var componentsX = goX.GetComponents<Component>();
        var componentsY = goY.GetComponents<Component>();

        if (componentsX.Length != componentsY.Length)
            return componentsX.Length > componentsY.Length ? 1 : -1;

        for (int i = 0; i < componentsX.Length; i++)
        {
            var compX = componentsX[i];
            var compY = componentsY[i];
             if(!compX.GetType().Equals(compY.GetType()))
                 return x.GetType().Name.CompareTo(y.GetType().Name);
        }

        return 0;
    }

    Editor GetOrCreateEditor(Object o)
    {
        Editor gameObjectEditor;
        if (editors.TryGetValue(o, out gameObjectEditor))
        {
            return gameObjectEditor;
        }
        
        Type customEditorType;
        customEditors.TryGetValue(o.GetType(), out customEditorType);

        gameObjectEditor = Editor.CreateEditor(o, customEditorType);
        editors.Add(o,gameObjectEditor);
        return gameObjectEditor;
    }

    void FindAllCustomInspectors()
    {
        if (customEditors.Count > 0)
            return;
        
        var assemblies = AppDomain.CurrentDomain.GetAssemblies();
        foreach (var assembly in assemblies)
        {
            var types = assembly.GetTypes();
            foreach (var type in types)
            {
                if (!type.IsSubclassOf(typeof(Editor)))
                    continue;

                var customEditorType = typeof(CustomEditor);
                var attributes = type.GetCustomAttributes(customEditorType);

                foreach (var attribute in attributes)
                {
                    if (attribute == null)
                        continue;
                
                    var fieldInfo = customEditorType.GetField("m_InspectedType", BindingFlags.NonPublic | BindingFlags.Instance);
                    var inspectedType = fieldInfo.GetValue(attribute) as System.Type;
                    if (inspectedType == null)
                    {
                        //   Debug.LogError("Type:" + type + " inspector null");
                        continue;
                    }

                    if (customEditors.ContainsKey(inspectedType))
                    {
//                    Debug.LogWarning("Type:" + inspectedType + " already has editor:" + customEditors[inspectedType]);
                        continue;
                    }
                    
                    if(!customEditors.ContainsKey(inspectedType))
                        customEditors.Add(inspectedType,type);
                }                    
            }
        }
    }
    
    private Texture2D CreateTexture2D( int width, int height, Color color )
    {
        var pixels = new Color[width * height];
        for(var i=0;i<pixels.Length;i++)
        {
            pixels[i] = color;
        }
        
        var result = new Texture2D( width, height );
        result.SetPixels( pixels );
        result.Apply();
        return result;
    }

}
