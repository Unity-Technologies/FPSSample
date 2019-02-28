using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.Profiling;

public class NetworkProfiler : EditorWindow
{
    void OnEnable()
    {
        m_TooltipStyle = new GUIStyle();
        m_TooltipStyle.alignment = TextAnchor.UpperLeft;
        m_TooltipStyle.normal.textColor = Color.white;
        m_TooltipStyle.normal.background = Texture2D.whiteTexture;
        m_TooltipStyle.border = new RectOffset(0, 0, 0, 0);
    }


    [MenuItem("FPS Sample/Windows/Network Profiler")]
    public static void ShowWindow()
    {
        GetWindow<NetworkProfiler>(false, "Network Profiler", true);
    }
    
    private string SizeToString(int size)
    {
        if (m_ShowByteSizes)
            return string.Format("{0:0.00}", size / 8.0f);
        else
            return "" + size;
    }

    void OnGUI()
    {
        if (Game.game == null)
        {
            GUILayout.Label("Game not running");
            return;
        }

        var serverGameLoop = Game.GetGameLoop<ServerGameLoop>();
        if (serverGameLoop == null)
        {
            GUILayout.Label("Not a server");
            return;
        }

        var networkServer = serverGameLoop.GetNetworkServer();

        // server pane
        float serverPaneWidth = position.width;
        float fieldIdWidth = serverPaneWidth * 0.03f;
        float fieldNameWidth = serverPaneWidth * 0.1f;
        float fieldTypeDesc = serverPaneWidth * 0.15f;
        float fieldValueDesc = serverPaneWidth * 0.16f;
        float fieldPredictionDesc = serverPaneWidth * 0.16f;
        float fieldDeltaDesc = serverPaneWidth * 0.17f;
        float fieldSentSize = serverPaneWidth * 0.15f;

        var leftAlignStyle = new GUIStyle(GUI.skin.GetStyle("Label"));
        leftAlignStyle.alignment = TextAnchor.UpperLeft;
        GUI.skin.GetStyle("Label").alignment = TextAnchor.UpperRight;

        GUILayout.BeginVertical(GUILayout.Width(serverPaneWidth));
        m_ScrollPos0 = GUILayout.BeginScrollView(m_ScrollPos0);
        GUILayout.Label("serverTickRate: " + networkServer.serverInfo.serverTickRate);

        // server stats
        GUILayout.BeginHorizontal();
        GUILayout.Label("Tick: " + networkServer.serverTime);
        GUILayout.Label("Tick rate: " + networkServer.serverInfo.serverTickRate);
        GUILayout.Label("Number of entity types: " + networkServer.GetEntityTypes().Count);
        GUILayout.Label("Number of entities: " + networkServer.NumEntities);
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        m_ShowRawData = GUILayout.Toggle(m_ShowRawData, "Show raw data");
        m_ShowByteSizes = GUILayout.Toggle(m_ShowByteSizes, "Show sizes in bytes");
        GUILayout.EndHorizontal();

        Vector2 mousePosition = Event.current.mousePosition;
        string tooltip = "";

        GUILayout.Space(10.0f);
        GUILayout.Label("Schemas:", leftAlignStyle);
        
        // fields
        m_ScrollPos1 = GUILayout.BeginScrollView(m_ScrollPos1);
        foreach (var entityTypePair in networkServer.GetEntityTypes())
        {
            var entityType = entityTypePair.Value;
            var schema = entityType.schema;
            GUILayout.Label(entityType.name + " Schema:", leftAlignStyle);

            // header
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label("Id", GUILayout.Width(fieldIdWidth));
                GUILayout.Label("Name", GUILayout.Width(fieldNameWidth));
                GUILayout.Label("Type", GUILayout.Width(fieldTypeDesc));
                GUILayout.Label("Value", GUILayout.Width(fieldValueDesc));
                GUILayout.Label("Prediction", GUILayout.Width(fieldPredictionDesc));
                GUILayout.Label("Delta", GUILayout.Width(fieldDeltaDesc));
                GUILayout.Label(m_ShowByteSizes ? "Sent/bytes" : "Sent/bits", GUILayout.Width(fieldSentSize));
                GUILayout.EndHorizontal();
            }

            int totalSchemaSize = 0;
            int numFields = schema.numFields;
            for (int i = 0; i < numFields; i++)
            {
                var field = schema.fields[i];
                NetworkSchema.FieldStatsBase stats = field.stats;

                string typeDesc;
                string valueDesc = stats != null ? stats.GetValue(m_ShowRawData) : "";
                if(field.fieldType == NetworkSchema.FieldType.String || field.fieldType == NetworkSchema.FieldType.ByteArray)
                {
                    typeDesc = field.arraySize + "byte " + field.fieldType;
                }
                else
                {
                    typeDesc = field.bits + "bit " + field.fieldType;
                }

                GUILayout.BeginHorizontal();

                GUILayout.Label("" + i, GUILayout.Width(fieldIdWidth));
                GUILayout.Label(field.name, GUILayout.Width(fieldNameWidth));
                GUILayout.Label(typeDesc, GUILayout.Width(fieldTypeDesc));
                
                GUILayout.Label(valueDesc, GUILayout.Width(fieldValueDesc));
                if (Event.current.type == EventType.Repaint && GUILayoutUtility.GetLastRect().Contains(Event.current.mousePosition))
                {
                    tooltip = string.Format("Value: {0}\nRange: ({1}-{2})", stats.GetValue(m_ShowRawData), stats.GetValueMin(m_ShowRawData), stats.GetValueMax(m_ShowRawData));
                }
                
                if(field.delta)
                {
                    GUILayout.Label(stats.GetPrediction(m_ShowRawData), GUILayout.Width(fieldPredictionDesc));
                    if (Event.current.type == EventType.Repaint && GUILayoutUtility.GetLastRect().Contains(Event.current.mousePosition))
                    {
                        tooltip = string.Format("Prediction: {0}\nRange: ({1}-{2})", stats.GetPrediction(m_ShowRawData), stats.GetPredictionMin(m_ShowRawData), stats.GetPredictionMax(m_ShowRawData));
                    }
                    
                    GUILayout.Label(stats.GetDelta(m_ShowRawData), GUILayout.Width(fieldDeltaDesc));
                    if (Event.current.type == EventType.Repaint && GUILayoutUtility.GetLastRect().Contains(Event.current.mousePosition))
                    {
                        tooltip = string.Format("Delta: {0}\nRange: ({1}-{2})", stats.GetDelta(m_ShowRawData), stats.GetDeltaMin(m_ShowRawData), stats.GetDeltaMax(m_ShowRawData));
                    }
                }
                else
                {
                    GUILayout.Label("", GUILayout.Width(fieldPredictionDesc));
                    GUILayout.Label("", GUILayout.Width(fieldDeltaDesc));
                }
                
                GUILayout.Label(SizeToString(stats.GetNumBitsWritten()), GUILayout.Width(fieldSentSize));
                if(Event.current.type == EventType.Repaint && GUILayoutUtility.GetLastRect().Contains(Event.current.mousePosition))
                {
                    if (m_ShowByteSizes)
                    {
                        tooltip = string.Format("Sent: {0:0.00} bytes\n" +
                                                "Sends: {1}\n" +
                                                "Bytes per send: {2:0.00}", stats.GetNumBitsWritten() / 8.0f, stats.GetNumWrites(), stats.GetNumBitsWritten() / ((float)stats.GetNumWrites() * 8.0f));
                    }
                    else
                    {
                        tooltip = string.Format("Sent: {0} bits\n" +
                                                "Sends: {1}\n" +
                                                "Bits per send: {2:0.00}", stats.GetNumBitsWritten(), stats.GetNumWrites(), stats.GetNumBitsWritten() / (float)stats.GetNumWrites());
                    }
                    
                }

                totalSchemaSize += stats.GetNumBitsWritten();


                GUILayout.EndHorizontal();  
            }

            // Total row
            GUILayout.BeginHorizontal();
            GUILayout.Label("", GUILayout.Width(fieldIdWidth));
            GUILayout.Label("Total", GUILayout.Width(fieldNameWidth));
            GUILayout.Label("", GUILayout.Width(fieldTypeDesc));
            GUILayout.Label("", GUILayout.Width(fieldValueDesc));
            GUILayout.Label("", GUILayout.Width(fieldPredictionDesc));
            GUILayout.Label("", GUILayout.Width(fieldDeltaDesc));
            GUILayout.Label(SizeToString(totalSchemaSize), GUILayout.Width(fieldSentSize));
            GUILayout.EndHorizontal();
        }
        GUILayout.EndScrollView();
        GUILayout.EndScrollView();
        GUILayout.EndVertical();
        
        if(tooltip != "")
        {
            var content = new GUIContent(tooltip);
            Vector2 size = m_TooltipStyle.CalcSize(content);
            GUI.backgroundColor = Color.gray;

            GUI.Box(new Rect(mousePosition.x - size.x, mousePosition.y, size.x, size.y), tooltip, m_TooltipStyle);
        }
    }

    void Update()
    {
        //TODO: some condition
        Repaint();
    }

    Vector2 m_ScrollPos0;
    Vector2 m_ScrollPos1;
    bool m_ShowRawData;
    bool m_ShowByteSizes;
    GUIStyle m_TooltipStyle;
}
