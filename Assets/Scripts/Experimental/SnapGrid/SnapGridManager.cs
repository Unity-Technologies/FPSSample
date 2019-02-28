using UnityEditor;
using UnityEngine;

#if UNITY_EDITOR

[InitializeOnLoad]
public class SnapGridManager
{
    private static Transform snapGridRef;
    private static bool toolActive;
    private static float gridSize = 1;
    private static bool gridButtonDown = false;
    
    static SnapGridManager()
    {
        SceneView.onSceneGUIDelegate -= OnSceneGuiDelegate;       
        SceneView.onSceneGUIDelegate += OnSceneGuiDelegate;       
    }

    private static void OnSceneGuiDelegate(SceneView sceneview)
    {
        if (Event.current.isKey && Event.current.keyCode == KeyCode.G && !Event.current.control && !Event.current.alt)
        {
            var newButtonDown = Event.current.type == EventType.KeyDown;
            if (newButtonDown != gridButtonDown)
            {
                gridButtonDown = newButtonDown;
            } 
            Event.current.Use();
        }
         
        
//        // If no grid set, default to one from scene
//        if (snapGridRef == null)
//        {
//            var grids = GameObject.FindObjectsOfType<SnapGrid>();
//            if (grids.Length > 0)
//                snapGridRef = grids[0].transform;
//        }
        
        // Test for grid tool start
        if (!toolActive && gridButtonDown)
        {
            toolActive = true;
            Tools.current = Tool.None;
            SceneView.RepaintAll();
            Debug.Log("Start");
        }              

        if(toolActive && Tools.current != Tool.None)
        {
            Debug.Log("Stop");
            toolActive = false;
            SceneView.RepaintAll();
        }

        if (!toolActive)
            return;

        if (Event.current.isMouse && Event.current.button == 1 
                                  && Event.current.type == EventType.MouseDown
                                  && gridButtonDown)
        {
            var picked = HandleUtility.PickGameObject(Event.current.mousePosition, false);
            while (picked != null && picked.hideFlags.HasFlag(HideFlags.NotEditable))
            {
                picked = picked.transform.parent.gameObject;
            }
            
            if(picked != null)
            {
                snapGridRef = picked.transform;
                Event.current.Use ();
            }
        }
        
        if (Event.current.isScrollWheel)
        {
            Event.current.Use();

            if (Event.current.delta.y > 0f)
                gridSize *= 2;
            else
                gridSize *= 0.5f;
        }


        
        var snapObject = Selection.activeGameObject;
        if (snapObject != null && snapGridRef != null)
        {
            var pos = snapObject.transform.position;
            
            var newPos = Handles.PositionHandle(pos, snapGridRef.transform.rotation);
            var deltaX = newPos.x - pos.x;
            var deltaY = newPos.y - pos.y;
            var deltaZ = newPos.z - pos.z;   
        
            var localPos = snapGridRef.gameObject.transform.InverseTransformPoint(pos);
            var newLocalPos = snapGridRef.gameObject.transform.InverseTransformPoint(newPos);
            var snappedLocalPos = CalcGridPos(newLocalPos, new Vector3(gridSize,gridSize,gridSize));

            var axisCount = 0;
            if (Mathf.Abs(deltaX) > Mathf.Epsilon)
            {
                localPos.x = snappedLocalPos.x;
                axisCount++;
            }
            if (Mathf.Abs(deltaY) > Mathf.Epsilon)
            {
                localPos.y = snappedLocalPos.y;
                axisCount++;
            }
            if (Mathf.Abs(deltaZ) > Mathf.Epsilon)
            {
                localPos.z = snappedLocalPos.z;
                axisCount++;
            }

            if (axisCount > 1)
            {
                localPos = snappedLocalPos;
            }
        
            var newWorldPos = snapGridRef.gameObject.transform.TransformPoint(localPos);

            if (Vector3.Distance(newWorldPos, snapObject.transform.position) > 0.001f)
            {
                Undo.RecordObject(snapObject.transform, "Snap to grid");
                snapObject.transform.position = newWorldPos;    
            }
        
            var snappedWorldPos = snapGridRef.gameObject.transform.TransformPoint(snappedLocalPos);
        
            Handles.color = Color.white;
            Handles.DrawLine(snapObject.transform.position, snappedWorldPos);
            
            DrawGrid(snapGridRef.transform, gridSize, snappedLocalPos, 21, Color.gray);
        }
        
        if (snapGridRef != null)
        {
            DrawGrid(snapGridRef.transform, gridSize, Vector3.zero, 5, Color.yellow);
        }

        Handles.BeginGUI();
        
        GUILayout.BeginArea(new Rect(10, 10, 240, 152), "Grid Snap Tool", EditorStyles.helpBox);
        
        GUILayout.Space(20);
        
        GUILayout.ExpandWidth(false);
        EditorGUIUtility.labelWidth = 70;
        
        gridSize = EditorGUILayout.FloatField("Size:",gridSize);
        
        string[] options = null;
        var snapGrids = GameObject.FindObjectsOfType<SnapGrid>();
        var selected = snapGrids.Length;
        options = new string[snapGrids.Length + 1];
        for (var i = 0; i < snapGrids.Length; i++)
        {
            options[i] = snapGrids[i].name;
            if (snapGrids[i].transform == snapGridRef)
                selected = i;
        }
        options[snapGrids.Length] = "<custom>";
        
        var newSelected = EditorGUILayout.Popup("Grid:",selected, options);
        if (newSelected != selected)
        {
            snapGridRef = snapGrids[newSelected].transform;
        }

        if (snapGridRef != null && snapObject != null)
        {
            // Pos
            var localPos = snapGridRef.gameObject.transform.InverseTransformPoint(snapObject.transform.position);
            localPos = Round(localPos, 2);
            
            var newPos = EditorGUILayout.Vector3Field("Local Pos:",localPos);
            if (Vector3.Distance(newPos,localPos) > 0.001f)
            {
                Undo.RecordObject(snapObject.transform, "change transform");
                snapObject.transform.position = snapGridRef.gameObject.transform.TransformPoint(newPos);
            }            
            
            // Rot
            var invGridRot = Quaternion.Inverse(snapGridRef.gameObject.transform.rotation);
            var localRot = invGridRot*snapObject.transform.rotation;            
            var localRotEuler = Round(localRot.eulerAngles, 2);

            var newLocalRotEuler = EditorGUILayout.Vector3Field("Local Rot:",localRotEuler);
            if (Vector3.Distance(newLocalRotEuler,localRot.eulerAngles) > 0.001f)
            {
                Undo.RecordObject(snapObject.transform, "change transform");

                var newLocalRot = Quaternion.Euler(newLocalRotEuler);
                var newRot = snapGridRef.gameObject.transform.rotation * newLocalRot;

                snapObject.transform.rotation = newRot;
            }   
        }
        else
        {
            GUI.enabled = false;
            EditorGUILayout.Vector3Field("Local Pos:",Vector3.zero);
            EditorGUILayout.Vector3Field("Local Rot:",Vector3.zero);
            GUI.enabled = true;
        }
        
        if(GUILayout.Button("Snap Selected") && snapGridRef != null)
        {
            foreach (var go in Selection.gameObjects)
            {
                Undo.RecordObject(go.transform, "snap"); 
            
                var pos = go.transform.position;
                var localPos = snapGridRef.gameObject.transform.InverseTransformPoint(pos);
                var snappedLocalPos = CalcGridPos(localPos, new Vector3(gridSize,gridSize,gridSize));
                var snappedWorldPos = snapGridRef.gameObject.transform.TransformPoint(snappedLocalPos);
                go.transform.position = snappedWorldPos;
                
                var invGridRot = Quaternion.Inverse(snapGridRef.gameObject.transform.rotation);
                var localRot = invGridRot*snapObject.transform.rotation;
                var localRotEuler = localRot.eulerAngles;
                localRotEuler.x =  Mathf.Round(localRotEuler.x / 90) * 90;
                localRotEuler.y =  Mathf.Round(localRotEuler.y / 90) * 90;
                localRotEuler.z =  Mathf.Round(localRotEuler.z / 90) * 90;
                localRot = Quaternion.Euler(localRotEuler);
                var newRot = snapGridRef.gameObject.transform.rotation * localRot;
                
                go.transform.rotation = newRot;
            }
        }
        
        
        GUILayout.EndArea();
        
        if (gridButtonDown)
        {
            var mousePos = Event.current.mousePosition;
            GUILayout.BeginArea(new Rect(mousePos, new Vector2(100,40)),EditorStyles.helpBox);
            GUILayout.Label("Right click");
            GUILayout.Label("Select grid ref");
            GUILayout.EndArea();
        }
        Handles.EndGUI();
    }

    static Vector3 Round(Vector3 v, int decimals)
    {
        var m = Mathf.Pow(10.0f, (float)decimals);
        return new Vector3(Mathf.Round(v.x*m)/m,Mathf.Round(v.y*m)/m,Mathf.Round(v.z*m)/m);  
    }
    
    static void DrawGrid(Transform gridRef, float size, Vector3 localCenter, int lineCount, Color color)
    {

        var offset = (lineCount - 1) / 2;

        var yUp = gridRef.forward.y < gridRef.up.y;

        var localSize = yUp ? new Vector3(size, 0, size) : new Vector3(size, size, 0);
        var startPosLocal =  localCenter + localSize * offset;
        var startPosWorld = gridRef.TransformPoint(startPosLocal);

        var forward = gridRef.forward.y < gridRef.up.y ? gridRef.forward : gridRef.up;
        var right = gridRef.right;

        var zTest = Handles.zTest;
        Handles.zTest = UnityEngine.Rendering.CompareFunction.LessEqual;
        Handles.color = color;
        
        var pos = startPosWorld;
        for (var i = 0; i < lineCount; i++)
        {
            Handles.DrawLine(pos + forward*size, pos - forward*lineCount*size);
            pos -= right * size;
        }

        pos = startPosWorld;
        for (var i = 0; i < lineCount; i++)
        {
            Handles.DrawLine(pos + right*size, pos - right*lineCount*size);
            pos -= forward * size;
        }  
        Handles.zTest = zTest;
    }
    
    static Vector3 CalcGridPos(Vector3 pos, Vector3 size)
    {
        return new Vector3( Mathf.Round(pos.x / size.x)*size.x, Mathf.Round(pos.y / size.y)*size.y,Mathf.Round(pos.z / size.z)*size.z);
    }
}

#endif