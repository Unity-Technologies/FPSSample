using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(LightProbePlacement))]
public class LightProbePlacementEditor : Editor
{
    void OnEnable()
    {
        m_lightProbePlacement = target as LightProbePlacement;
        m_lightProbeGroup = m_lightProbePlacement.gameObject.GetComponent<LightProbeGroup>();
        Debug.Assert(m_lightProbeGroup != null);
    }


    public override void OnInspectorGUI()
    {
        Color oldColor = GUI.color;
        GUI.color = m_lightProbePlacement.placementEnabled ? Color.green : Color.red;
        if (GUILayout.Button("PLACE [L key]"))
        {
            m_lightProbePlacement.placementEnabled = !m_lightProbePlacement.placementEnabled;
        }
        GUI.color = oldColor;

        m_lightProbePlacement.placementHeight = EditorGUILayout.FloatField("Placement Height", m_lightProbePlacement.placementHeight);
    }

    void OnSceneGUI()
    {
        Event currentEvent = Event.current;
        //if (currentEvent.isMouse || currentEvent.isKey)
        //    Debug.Log("currentEvent:" + currentEvent);

        if (!m_lightProbePlacement.placementEnabled)
            return;

        //        if(currentEvent.type == EventType.MouseDown && currentEvent.shift)
        if (currentEvent.type == EventType.KeyUp && currentEvent.keyCode == KeyCode.L)
        {
            var ray = HandleUtility.GUIPointToWorldRay(currentEvent.mousePosition);
            RaycastHit hit;
            if (Physics.Raycast(ray, out hit, 1000.0f))
            {
                Vector3 pos = hit.point + m_lightProbePlacement.placementHeight * hit.normal;

                // Create new position list
                Vector3[] positions = new Vector3[m_lightProbeGroup.probePositions.Length + 1];
                for (int i = 0; i < m_lightProbeGroup.probePositions.Length; i++)
                    positions[i] = m_lightProbeGroup.probePositions[i];
                positions[positions.Length - 1] = pos;

                // Set positions on group
                Undo.RecordObject(m_lightProbeGroup, "Add light probe");
                m_lightProbeGroup.probePositions = positions;

                currentEvent.Use();
            }
        }

        if(GUI.changed)
            EditorUtility.SetDirty(target);
    }

    LightProbePlacement m_lightProbePlacement;
    LightProbeGroup m_lightProbeGroup;
}
