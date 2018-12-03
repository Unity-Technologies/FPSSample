using System.Runtime.CompilerServices;
using Unity.Entities;
using UnityEditor;
using UnityEngine;

public class StateHistoryInspectorWindow : EditorWindow
{
    int m_selectedColumnIndex; 
    int m_selectedRowIndex;
    
    int columnWidth = 120;

    Vector2 m_inspectorScrolllPosition;
    
    public void SetResult(int column, int row)
    {
        m_selectedColumnIndex = column;
        m_selectedRowIndex = row;
        Repaint();
    }
    
    void OnGUI()
    {
        

        if (ReplicatedEntityCollection.SampleHistory)
        {
            GUILayout.Label("Sampling ...");
            return;
        }
            
        var clientGameLoop = Game.GetGameLoop<ClientGameLoop>();
        if (clientGameLoop == null)
            return;
        var clientGameWorld = clientGameLoop.GetClientGameWorld();
        if(clientGameWorld == null)
            return;
        ReplicatedEntityModuleClient repEntityModule = clientGameWorld.ReplicatedEntityModule;
        if(repEntityModule == null)
            return;

        var netId = repEntityModule.GetNetIdFromEntityIndex(m_selectedColumnIndex);
        if (netId == -1)
            return;

        var repData = repEntityModule.GetReplicatedDataForNetId(netId);
        
        // TODO (mogensh) also show non predicted data
        if (!repEntityModule.IsPredicted(m_selectedColumnIndex))
        {
            GUILayout.Label("no predicted data");
            return;
        }

        var selectedTick = repEntityModule.GetSampleTick(m_selectedRowIndex);
        var predictStartTick = repEntityModule.GetLastServerTick(m_selectedRowIndex) + 1;
        var predictCount = selectedTick - predictStartTick + 1;

        GUILayout.BeginHorizontal();

        var entityName = "NetId:" + netId + " E(" + repData.entity.Index + ":" + repData.entity.Version + ")";
        var goName = repData.gameObject != null ? "(" + repData.gameObject.name + ")" : "";
        GUILayout.Label("Entity:" + entityName + goName);
        GUILayout.Label("Predict:" + predictStartTick + " to: " + selectedTick + "(" + predictCount + ")");
        columnWidth = EditorGUILayout.IntField("Column width", columnWidth);
        
        GUILayout.FlexibleSpace();
        GUILayout.EndHorizontal();


        if (predictCount > 30)
        {
            GUILayout.Label("Predict count HIGHT!");
            return;
        }
        
        
        
        
        GUILayout.BeginHorizontal();
        
        // Property names
        var scrollPos = GUILayout.BeginScrollView(new Vector2(0,m_inspectorScrolllPosition.y));
        m_inspectorScrolllPosition.y = scrollPos.y;
        {
            GUILayout.BeginVertical(GUILayout.Width(200));
            GUILayout.Label("Properties:");
            int predictedHandlerCount = repData.predictedArray.Length;
            for (int i = 0; i < predictedHandlerCount; i++)
            {
                var sampleIndex = repEntityModule.FindSampleIndexForTick(selectedTick);
                var predictedState = repData.predictedArray[i].GetPredictedState(sampleIndex, 0);
                DrawObjectFieldNames(repData.predictedArray[i].GetEntity(), predictedState);
                
            }
            GUILayout.EndVertical();
        }
        GUILayout.EndScrollView();
        
        m_inspectorScrolllPosition = GUILayout.BeginScrollView(m_inspectorScrolllPosition);
        GUILayout.BeginHorizontal();
        
        for (int i = predictCount - 1; i >= 0; i--)
        {
            int predictTick = selectedTick - i;
            GUILayout.BeginVertical();
            GUILayout.Label("Tick:" + predictTick, GUILayout.Width(columnWidth));
            int predictedHandlerCount = repData.predictedArray.Length;
            for (int j = 0; j < predictedHandlerCount; j++)
            {
                var sampleIndex = repEntityModule.FindSampleIndexForTick(selectedTick);
                var predictedState = repData.predictedArray[j].GetPredictedState(sampleIndex, i);

                var bg = GUI.color;
                GUI.color = i == 0 ? Color.cyan : new Color(0.0f, 0.7f, 0.7f, 1f);
                DrawObjectFieldValues(predictedState);
                GUI.color = bg;
            }
            GUILayout.EndVertical();  
        }

        {
            GUILayout.BeginVertical();
            GUILayout.Label("ServerState:");
            int predictedHandlerCount = repData.predictedArray.Length;
            for (int i = 0; i < predictedHandlerCount; i++)
            {
                var serverState = repData.predictedArray[i].GetServerState(selectedTick);
                
                if(serverState == null)
                    continue;

                var sampleIndex = repEntityModule.FindSampleIndexForTick(selectedTick);
                var verified = repData.predictedArray[i].VerifyPrediction(sampleIndex, selectedTick);

                var bg = GUI.color;
                GUI.color = verified ? Color.green : Color.red;
                DrawObjectFieldValues(serverState);
                GUI.color = bg;
            }
            GUILayout.EndVertical();
        }
        
        GUILayout.FlexibleSpace();
        GUILayout.EndHorizontal();
        GUILayout.EndScrollView();
        GUILayout.EndHorizontal();
    }

    void DrawObjectFieldNames(Entity entity, object o)
    {
        var type = o.GetType();
        var fields = type.GetFields();
                
        GUILayout.Label("E(" + entity.Index + ":" + entity.Version + ") " + type.Name, EditorStyles.boldLabel);
        foreach (var field in fields)
        {
            GUILayout.Label(field.Name, EditorStyles.miniLabel);
        }
    }
    
    void DrawObjectFieldValues(object o)
    {
        var type = o.GetType();
        var fields = type.GetFields();
                
        GUILayout.Label("--------", EditorStyles.boldLabel);
        foreach (var field in fields)
        {
            var val = field.GetValue(o);
            
            GUILayout.Label(val.ToString(), EditorStyles.miniLabel);
        }
    }
}
