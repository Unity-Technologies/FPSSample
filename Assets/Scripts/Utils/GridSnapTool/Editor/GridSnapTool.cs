using UnityEditor;
using UnityEngine;


[InitializeOnLoad]
public class GridSnapTool
{
    private static bool isActive;
    private static bool isOnSceneGUIRegistered;
    private static float gridSize = 12.5f;
    static GridSnapTool()
    {
        CheckSceneGUIRegistration();
    }

    private const string ToggleName = "FPS Sample/GridSnapTool";
    [MenuItem(ToggleName)]
    private static void ToggleGridSnapTool()
    {
        isActive = !Menu.GetChecked(ToggleName);
        CheckSceneGUIRegistration();
    }

    
    [MenuItem("FPS Sample/Hotkeys/Snap selected to grid &s")]
    static void SnapSelectedHotkey()
    {
        SnapSelected();
    }
    

    
    [MenuItem(ToggleName, true)]
    private static bool ToggleGridSnapToolValidate()
    {
        Menu.SetChecked(ToggleName, isActive);
        CheckSceneGUIRegistration();
        return true;
    }


    static void CheckSceneGUIRegistration()
    {
        if (isActive && !isOnSceneGUIRegistered)
        {
            isOnSceneGUIRegistered = true;
            SceneView.onSceneGUIDelegate += OnSceneGUI;
        }
        if (!isActive && isOnSceneGUIRegistered)
        {
            isOnSceneGUIRegistered = false;
            SceneView.onSceneGUIDelegate -= OnSceneGUI;
        }
        SceneView.RepaintAll();
    }

    static void SnapSelected()
    {
        foreach (var gameObject in Selection.gameObjects)
        {
            var position = gameObject.transform.position;
            var gridPos = CalcGridPos(position, gridSize);
            Undo.RecordObject(gameObject.transform, "Snap");
            gameObject.transform.position = gridPos;
        }

    }
    
    static void OnSceneGUI(SceneView sceneview)
    {
        Handles.BeginGUI();
        
        GUILayout.BeginArea(new Rect(10, 10, 100, 90),EditorStyles.helpBox);
        GUILayout.Label("Grip Snap Tool");
        GUILayout.Label(string.Format("Grid:{0:0.00}",gridSize));
        if (GUILayout.Button("Snap [Alt+S]"))
        {
            SnapSelected();
        }
        if (GUILayout.Button("Close"))
        {
            isActive = false;
            CheckSceneGUIRegistration();
        }
            
//        gridSize = EditorGUILayout.FloatField("Grip size", gridSize);
        GUILayout.EndArea();
        Handles.EndGUI();
//        Handles.Label(new Vector3(10,0,0), )

        var gridDrawSize = 1;
        
        foreach (var gameObject in Selection.gameObjects)
        {
            var position = gameObject.transform.position;
            
            var gridPos = CalcGridPos(position, gridSize);

            Debug.DrawLine(position,gridPos,Color.yellow);
            
            Debug.DrawLine(gridPos-Vector3.right*gridDrawSize,gridPos+Vector3.right*gridDrawSize,Color.red);
            Debug.DrawLine(gridPos-Vector3.up*gridDrawSize,gridPos+Vector3.up*gridDrawSize,Color.green);
            Debug.DrawLine(gridPos-Vector3.forward*gridDrawSize,gridPos+Vector3.forward*gridDrawSize,Color.blue);
        }
    }

    static Vector3 CalcGridPos(Vector3 pos, float gridSize)
    {
        return new Vector3( Mathf.Round(pos.x / gridSize), Mathf.Round(pos.y / gridSize) ,Mathf.Round(pos.z / gridSize))*gridSize;
    }
}
