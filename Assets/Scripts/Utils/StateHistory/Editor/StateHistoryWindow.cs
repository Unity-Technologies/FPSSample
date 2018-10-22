using System;
using System.Reflection;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;


public class StateHistoryWindow : EditorWindow
{

    [MenuItem("FPS Sample/Windows/State History")]
    public static void ShowWindow()
    {
        GetWindow<StateHistoryWindow>(false, "State History", true);
    }

    private void OnEnable()
    {
        StateHistorySampler.Capture += OnCapture;
        EditorApplication.playModeStateChanged += OnPlaymodeChanged;
        EditorApplication.quitting += OnQuitting;
    }

    void OnDisable()
    {
        StateHistorySampler.Capture -= OnCapture;
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

    private void OnCapture(StateHistorySampler.CaptureResult result)
    {
        //if(m_pauseOnCapture)
            //EditorApplication.isPaused = true;

        int endTick = result.firstTick + result.count - 1;

        if (m_captureResults.ContainsKey(endTick))
            m_captureResults[endTick] = result;
        else
            m_captureResults.Add(endTick, result);

        SetActiveResult(result);

        Repaint();
    }

    void SetActiveResult(StateHistorySampler.CaptureResult result)
    {
        if (result == m_activeResult)
            return;

        Reset();
        m_activeResult = result;
    }

    private void Reset()
    {
        m_selectedColumnIndex = -1;
        m_selectedRowIndex = 0;
        m_activeResult = null;
    }

    void OnGUI()
    {
        GUILayout.BeginVertical();
        DrawHeader();
        GUILayout.EndVertical();

        if (!StateHistory.Enabled)
            return;

        GUILayout.BeginHorizontal();

        DrawStates();

       // EditorGUILayout.LabelField("", GUI.skin.verticalSlider);

        DrawInspector();

        GUILayout.EndHorizontal();
    }

    void DrawHeader()
    {
        GUILayout.BeginHorizontal();

        bool enabled = GUILayout.Toggle(StateHistory.Enabled, "Enabled", EditorStyles.toolbarButton);
        if(enabled != StateHistory.Enabled)
        {
            StateHistory.Enabled = enabled;
        }

        StateHistorySampler.captureOnMispredict = GUILayout.Toggle(StateHistorySampler.captureOnMispredict, "Capture on mispredict", EditorStyles.toolbarButton);

//        m_pauseOnCapture = GUILayout.Toggle(m_pauseOnCapture, "Pause on capture", EditorStyles.toolbarButton);

        if (GUILayout.Button("SAMPLE [Ctrl+H]", EditorStyles.toolbarButton, GUILayout.MinWidth(200)))
        {
            StateHistorySampler.RequestCapture();
        }

        GUILayout.FlexibleSpace();

        // Capture selection
        {
            string[] options = new string[m_captureResults.Count];
            int i = 0;
            int selected = -1;
            foreach(var pair in m_captureResults)
            {
                if (pair.Value == m_activeResult)
                    selected = i;
                options[i] = pair.Key.ToString();
                i++;
            }

            GUILayout.Label("Show:", EditorStyles.toolbarButton);

            int newSelected = EditorGUILayout.Popup(selected, options, EditorStyles.toolbarDropDown, GUILayout.Width(80));
            if(newSelected != selected)
            {
                int key = int.Parse(options[newSelected]);
                SetActiveResult(m_captureResults[key]);
            }
        }

        GUILayout.EndHorizontal();
    }

    void DrawStates()
    {
        if (m_activeResult == null)
            return;

        int tickWidth = 40;
        int commandWidth = 20;
        int stateWidth = 100;

        // Handle arrow navigation
        Event e = Event.current;
        if (e.isKey && e.type == EventType.KeyDown)
        {
            bool repaint = false;
            if (e.keyCode == KeyCode.UpArrow && m_selectedRowIndex > 0)
            {
                m_selectedRowIndex--;
                repaint = true;
            }
                
            if (e.keyCode == KeyCode.DownArrow && m_selectedRowIndex < m_activeResult.count - 1)
            {
                m_selectedRowIndex++;
                repaint = true;
            }

            if (e.keyCode == KeyCode.LeftArrow && m_selectedColumnIndex > -1)
            {
                m_selectedColumnIndex--;
                repaint = true;
            }

            if (e.keyCode == KeyCode.RightArrow && m_selectedColumnIndex < m_activeResult.componentData.Count - 1)
            {
                m_selectedColumnIndex++;
                repaint = true;
            }

            if(repaint)
            {
                Repaint();
                EditorGUIUtility.ExitGUI();
            }
        }


        GUILayout.BeginVertical();

        // Headed
        GUILayout.BeginScrollView(m_statesScrolllPosition, GUIStyle.none, GUIStyle.none);
        GUILayout.BeginHorizontal();
        GUILayout.Label("Tick", GUILayout.Width(tickWidth));
        GUILayout.Label("Command", GUILayout.Width(commandWidth));
        foreach (var componentData in m_activeResult.componentData)
        {
            GUILayout.BeginVertical(GUILayout.Width(stateWidth));
            GUILayout.Label(componentData.component.gameObject.name, GUILayout.Width(stateWidth));
            GUILayout.Label(componentData.component.GetType().Name, GUILayout.Width(stateWidth));
            GUILayout.EndVertical();
        }
        GUILayout.EndHorizontal();
        GUILayout.EndScrollView();

        // State grid
        m_statesScrolllPosition = GUILayout.BeginScrollView(m_statesScrolllPosition);
        GUILayout.BeginHorizontal();

        string[] stringRows = new string[m_activeResult.count];


        // Ticks
        {
            for (int i = 0; i < m_activeResult.count; i++)
            {
                int tick = i + m_activeResult.firstTick;
                stringRows[i] = tick.ToString();
            }
            GUILayout.SelectionGrid(-1, stringRows, 1, GUILayout.Width(tickWidth));
        }

        // Command
        {
            int selectedIndex = m_selectedColumnIndex == -1 ? m_selectedRowIndex : -1;
            for (int i = 0; i < m_activeResult.count; i++)
            {
                stringRows[i] = "C";
            }
            int newIndex = GUILayout.SelectionGrid(selectedIndex, stringRows, 1, GUILayout.Width(commandWidth));
            if (newIndex != selectedIndex)
            {
                m_selectedRowIndex = newIndex;
                if (m_selectedColumnIndex != -1)
                {
                    m_selectedColumnIndex = -1;
                    Repaint();
                }
            }
        }


        // States
        for(int componentIndex=0;componentIndex< m_activeResult.componentData.Count;componentIndex++)
        {
            bool isSelectedColumn = m_selectedColumnIndex == componentIndex;
            int selectedIndex = isSelectedColumn ? m_selectedRowIndex : -1;

            StateHistorySampler.ComponentData componentData = m_activeResult.componentData[componentIndex];

            for (int i = 0; i < m_activeResult.count; i++)
            {
                bool state = componentData.states[i] != null;
                bool predicted = componentData.predictedStates[i] != null;
                bool predictionValid = componentData.predictionValid[i];
                stringRows[i] = !predictionValid ? "MISS" : predicted && state ? "P+S" : predicted ? "P" : state ? "S" : "";
            }

            int newIndex = GUILayout.SelectionGrid(selectedIndex, stringRows, 1, GUILayout.Width(stateWidth));
            if (newIndex != selectedIndex)
            {
                m_selectedRowIndex = newIndex;
                if (m_selectedColumnIndex != componentIndex)
                {
                    m_selectedColumnIndex = componentIndex;
                    Repaint();
                }
            }
        }

        GUILayout.FlexibleSpace();
        GUILayout.EndHorizontal();

        GUILayout.EndScrollView();

        GUILayout.EndVertical();

    }

    void DrawInspector()
    {
        int fieldNameWidth = 100;
        int fieldValueWitdh = 140;
        int totalWidth = fieldNameWidth + 2 * fieldValueWitdh+ 10;

        GUILayout.BeginVertical();

        m_inspectorScrolllPosition = GUILayout.BeginScrollView(m_inspectorScrolllPosition, GUILayout.Width(totalWidth));

        GUILayout.BeginHorizontal();
        GUILayout.Label("Field", EditorStyles.boldLabel, GUILayout.Width(fieldNameWidth));
        GUILayout.Label("Predicted", EditorStyles.boldLabel, GUILayout.Width(fieldValueWitdh));
        GUILayout.Label("State", EditorStyles.boldLabel, GUILayout.Width(fieldValueWitdh));
        GUILayout.EndHorizontal();

        if (m_activeResult != null)
        {
            //int tick = m_activeResult.firstTick + m_selectedRowIndex;
            System.Object stateObject = null;
            System.Object predictedObject = null;

            if (m_selectedColumnIndex == -1)
            {
                stateObject = m_activeResult.commands[m_selectedRowIndex];
            }
            else
            {
                if (m_selectedColumnIndex < m_activeResult.componentData.Count)
                {
                    StateHistorySampler.ComponentData data = m_activeResult.componentData[m_selectedColumnIndex];
                    stateObject = data.states[m_selectedRowIndex];
                    predictedObject = data.predictedStates[m_selectedRowIndex];
                }
            }

            System.Object typeReferenceObject = stateObject != null ? stateObject : predictedObject != null ? predictedObject : null;
            if (typeReferenceObject != null)
            {

                Type stateObjectType = typeReferenceObject.GetType();
                foreach (var field in stateObjectType.GetFields())
                {
                    GUILayout.BeginHorizontal();

                    GUILayout.Label(field.Name, GUILayout.Width(fieldNameWidth));

                    System.Object predictedVal = predictedObject != null ? field.GetValue(predictedObject) : "";
                    GUILayout.Label(predictedVal.ToString(), GUILayout.Width(fieldValueWitdh));

                    System.Object stateVal = stateObject != null ? field.GetValue(stateObject) : "";
                    GUILayout.Label(stateVal.ToString(), GUILayout.Width(fieldValueWitdh));

                    GUILayout.EndHorizontal();
                }
            }
        }

        GUILayout.EndScrollView();

        GUILayout.EndVertical();
    }

    //bool m_pauseOnCapture;

    Dictionary<int, StateHistorySampler.CaptureResult> m_captureResults = new Dictionary<int, StateHistorySampler.CaptureResult>();
    StateHistorySampler.CaptureResult m_activeResult;
    int m_selectedColumnIndex; // is -1 when commands selected
    int m_selectedRowIndex;


    Vector2 m_statesScrolllPosition;
    Vector2 m_inspectorScrolllPosition;
}