using System;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

[RequireComponent(typeof(HealthState))]
[DisallowMultipleComponent]
public class Character : MonoBehaviour
{
    [NonSerialized] public float eyeHeight = 1.8f;
    [NonSerialized] public string characterName;
    [NonSerialized] public HeroTypeAsset heroTypeData;
    [NonSerialized] public Entity presentation;    // Main char presentation used updating animation state 
    [NonSerialized] public List<CharacterPresentationSetup> presentations = new List<CharacterPresentationSetup>();
    
    [NonSerialized] public Vector3 m_TeleportToPosition;    
    [NonSerialized] public Quaternion m_TeleportToRotation;
    [NonSerialized] public bool m_TeleportPending;
  
    [NonSerialized] public int teamId = -1;       

    [NonSerialized] public float altitude; 
    [NonSerialized] public Collider groundCollider; 
    [NonSerialized] public Vector3 groundNormal;
    
    public void TeleportTo(Vector3 position, Quaternion rotation)
    {
        m_TeleportPending = true;
        m_TeleportToPosition = position;
        m_TeleportToRotation = rotation;
    }
    
    CharacterPredictedData[] historyBuffer;
    private int historyFirstIndex;
    private int historyCount;
    
    public void ShowHistory(int tick)
    {
        if(historyBuffer == null)
            historyBuffer = new CharacterPredictedData[30];
        
        var goe = GetComponent<GameObjectEntity>();

        var nextIndex = (historyFirstIndex + historyCount) % historyBuffer.Length;
        historyBuffer[nextIndex] = goe.EntityManager.GetComponentData<CharacterPredictedData>(goe.Entity);;
//        GameDebug.Log(state.locoState + " sprint:" + state.sprinting + " action:" + state.action);
            
        if (historyCount < historyBuffer.Length)
            historyCount++;
        else
            historyFirstIndex = (historyFirstIndex + 1) % historyBuffer.Length;


        for (int i = 0; i < historyCount; i++)
        {
            var index = (historyFirstIndex + i) % historyBuffer.Length;
            var state = historyBuffer[index];
            
            var y = 2 + i;
            
            {
                var color = (Color32)Color.HSVToRGB(0.21f*(int)state.locoState, 1, 1);
                var colorRGB = ((color.r >> 4) << 8) | ((color.g >> 4) << 4) | (color.b >> 4); 
                DebugOverlay.Write(2,y, "^{0}{1}",  colorRGB.ToString("X3"), state.locoState.ToString());
            }
            DebugOverlay.Write(14,y, state.sprinting == 1 ? "Sprint" : "no-sprint");
            {
                var color = (Color32)Color.HSVToRGB(0.21f*(int)state.action, 1, 1);
                var colorRGB = ((color.r >> 4) << 8) | ((color.g >> 4) << 4) | (color.b >> 4); 
                DebugOverlay.Write(26,y, "^{0}{1}:{2}",  colorRGB.ToString("X3"), state.action.ToString(), state.actionStartTick);
            }
        }

    }
}
