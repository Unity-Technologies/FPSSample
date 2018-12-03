using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;
using UnityEditor;


public class StateHistoryWindow : EditorWindow
{

    [MenuItem("FPS Sample/Windows/Replicated History")]
    public static void ShowWindow()
    {
        GetWindow<StateHistoryWindow>(false, "Replicated History", true);
    }

    private void OnEnable()
    {
//        StateHistorySampler.Capture += OnCapture;
        EditorApplication.playModeStateChanged += OnPlaymodeChanged;
        EditorApplication.quitting += OnQuitting;
    }

    void OnDisable()
    {
//        StateHistorySampler.Capture -= OnCapture;
        EditorApplication.playModeStateChanged -= OnPlaymodeChanged;
        EditorApplication.quitting -= OnQuitting;

    }

    void OnQuitting()        
    {
        Reset();
    }

    void OnPlaymodeChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.ExitingPlayMode)
            Reset();
    }

    private void Reset()
    {
        m_selectedColumn = -1;
        m_selectedRow = 0;
//        m_activeResult = null;
    }

    void OnGUI()
    {
        DrawHeader();
        DrawStates();
    }

    void DrawHeader()
    {
        GUILayout.BeginHorizontal();

        var buttonText = ReplicatedEntityCollection.SampleHistory ? "PAUSE" : "START";
        if (GUILayout.Button(buttonText)) 
            ReplicatedEntityCollection.SampleHistory = !ReplicatedEntityCollection.SampleHistory;

        m_stopOnMispredict = GUILayout.Toggle(m_stopOnMispredict, "Stop on mispredict");
        
        GUILayout.FlexibleSpace();

        GUILayout.EndHorizontal();
    }

    void DrawStates()
    {
        var clientGameLoop = Game.GetGameLoop<ClientGameLoop>();
        if (clientGameLoop == null)
            return;
        var clientGameWorld = clientGameLoop.GetClientGameWorld();
        if(clientGameWorld == null)
            return;
        ReplicatedEntityModuleClient repEntityModule = clientGameWorld.ReplicatedEntityModule;
        if(repEntityModule == null)
            return;
                                                  
        var sampleCount = repEntityModule.GetSampleCount();
        int entityCount = repEntityModule.GetEntityCount();
        

        
        if (ReplicatedEntityCollection.SampleHistory)
        {
            GUILayout.Label("Sampling ...");

            if (m_stopOnMispredict)
            {
                for (int iEntity = 0; iEntity < entityCount; iEntity++)
                {
                    for (int i = 0; i < sampleCount; i++)
                    {
                        int tick = repEntityModule.GetSampleTick(i);
        
                        var netId = repEntityModule.GetNetIdFromEntityIndex(iEntity);
                        if (netId == -1)
                            continue;

                        var repData = repEntityModule.GetReplicatedDataForNetId(netId);                        
                        
                        var sampleIndex = repEntityModule.FindSampleIndexForTick(tick);
                        bool predictionValid = repData.VerifyPrediction(sampleIndex, tick);
                        if (!predictionValid)
                        {
                            ReplicatedEntityCollection.SampleHistory = false;
                            Repaint();
                        }
                    }
                }
            }
            
            return;
        }

        
        
        
        int tickWidth = 120;
        int stateWidth = 100;

        // Handle arrow navigation
        Event e = Event.current;
        if (e.type == EventType.KeyDown)
        {
            bool repaint = false;
            if (e.keyCode == KeyCode.UpArrow && m_selectedRow > 0)
            {
                m_selectedRow--;
                repaint = true;
            }
                
            if (e.keyCode == KeyCode.DownArrow && m_selectedRow < sampleCount - 1)
            {
                m_selectedRow++;
                repaint = true;
            }

            if (e.keyCode == KeyCode.LeftArrow && m_selectedColumn > -1)
            {
                m_selectedColumn--;
                repaint = true;
            }

            if (e.keyCode == KeyCode.RightArrow && m_selectedColumn < entityCount - 1)
            {
                m_selectedColumn++;
                repaint = true;
            }

            if(repaint)
            {
                Repaint();
                UpdateInspector();
                EditorGUIUtility.ExitGUI();
            }
        }

        GUILayout.BeginVertical();

        // Headed
        GUILayout.BeginHorizontal();
        GUILayout.Label("Tick", GUILayout.Width(tickWidth));
        
        GUILayout.BeginScrollView(new Vector2(m_statesScrolllPosition.x,0), GUIStyle.none, GUIStyle.none);
        GUILayout.BeginHorizontal();

        for (int iEntity = 0; iEntity < entityCount; iEntity++)
        {
            var netId = repEntityModule.GetNetIdFromEntityIndex(iEntity);
            if (netId == -1)
                continue;
            var repData = repEntityModule.GetReplicatedDataForNetId(netId);                        


            GUILayout.BeginVertical(GUILayout.Width(stateWidth));
            
            GUILayout.Label("Net id:" + netId, GUILayout.Width(stateWidth));            
            
            var entityName = "Entity i:" + repData.entity.Index + " v:" + repData.entity.Version;
            GUILayout.Label(entityName, GUILayout.Width(stateWidth));
            var gameObjectName = repData.gameObject != null ? repData.gameObject.name : "";
            GUILayout.Label(gameObjectName, GUILayout.Width(stateWidth));
            GUILayout.EndVertical();
        }
        GUILayout.EndHorizontal();
        GUILayout.EndScrollView();
        
        GUILayout.EndHorizontal();

        // State grid
        GUILayout.BeginHorizontal();

        string[] stringRows = new string[sampleCount];

        // Ticks

        var scroll = GUILayout.BeginScrollView(new Vector2(0,m_statesScrolllPosition.y), GUILayout.Width(tickWidth));
        m_statesScrolllPosition.y = scroll.y;
        {
            for (int i = 0; i < sampleCount; i++)
            {
                int tick = repEntityModule.GetSampleTick(i);
                int lastServerTIck = repEntityModule.GetLastServerTick(i);
                stringRows[i] = i + " " + tick.ToString() + " P:" + (tick - lastServerTIck);
            }
            GUILayout.SelectionGrid(-1, stringRows, 1);
        }
        GUILayout.EndScrollView();


        m_statesScrolllPosition = GUILayout.BeginScrollView(m_statesScrolllPosition);
        GUILayout.BeginHorizontal();

        // States
        for (int iEntity = 0; iEntity < entityCount; iEntity++)
        {
            bool isSelectedColumn = m_selectedColumn == iEntity;
            int selectedIndex = isSelectedColumn ? m_selectedRow : -1;


            for (int i = 0; i < sampleCount; i++)
            {
                int tick = repEntityModule.GetSampleTick(i);
                
                var netId = repEntityModule.GetNetIdFromEntityIndex(iEntity);
                if (netId == -1)
                    continue;
                var repData = repEntityModule.GetReplicatedDataForNetId(netId);                 

                var isPredicted = repEntityModule.IsPredicted(iEntity);
                var hasState = repData.HasState(tick);

                if (isPredicted)
                {
                    var sampleIndex = repEntityModule.FindSampleIndexForTick(tick);
                    var predictionValid = repData.VerifyPrediction(sampleIndex, tick);
                    stringRows[i] = !predictionValid ? "MISS" : hasState ? "P+S" : "P";
                }
                else
                {
                    stringRows[i] = hasState ? "S" : "";
                }
            }

            int newIndex = GUILayout.SelectionGrid(selectedIndex, stringRows, 1, GUILayout.Width(stateWidth));
            
            if (newIndex != selectedIndex)
            {
                m_selectedRow = newIndex;
                if (m_selectedColumn != iEntity)
                {
                    m_selectedColumn = iEntity;
                }

                UpdateInspector();
                Repaint();
            }
        }
        GUILayout.FlexibleSpace();
        GUILayout.EndHorizontal();
        GUILayout.EndScrollView();

        GUILayout.EndHorizontal();

        GUILayout.EndVertical();

    }


    void UpdateInspector()
    {
        var wnd = GetWindow<StateHistoryInspectorWindow>(false, "State History Inspect", false);
        wnd.SetResult(m_selectedColumn,m_selectedRow);
    }

    int m_selectedColumn; 
    int m_selectedRow;
    Vector2 m_statesScrolllPosition;
    bool m_stopOnMispredict = false;
}