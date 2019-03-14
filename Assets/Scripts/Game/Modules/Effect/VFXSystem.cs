using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Experimental.VFX;

[DisableAutoCreation]
public class VFXSystem : ComponentSystem
{
    static readonly int positionID = Shader.PropertyToID("position");
    static readonly int targetPositionID = Shader.PropertyToID("targetPosition");
    static readonly int directionID = Shader.PropertyToID("direction");

    class EffectTypeData
    {
        public VisualEffect visualEffect;
        
        // TODO (mogensh) For performance reasons we want to stop effects that are "done". For now all effect use same timeout duration.  
        public float maxDuration = 4.0f;
        public bool active;
        public float lastTriggerTime;
        
        public VFXEventAttribute eventAttribute;
    }
    
//    struct EffectInstance
//    {
//        public VFXEventAttribute eventAttribute;
//    }

    struct PointEffectRequest
    {
        public float3 position;
        public float3 normal;
        public VisualEffectAsset asset;
    }

    struct LineEffectRequest
    {
        public float3 start;
        public float3 end;
        public VisualEffectAsset asset;
    }

    
    GameObject m_rootGameObject;
    List<PointEffectRequest> m_pointEffectRequests = new List<PointEffectRequest>(32);
    List<LineEffectRequest> m_lineEffectRequests = new List<LineEffectRequest>(32);
    Dictionary<VisualEffectAsset, EffectTypeData> m_EffectTypeData = new Dictionary<VisualEffectAsset, EffectTypeData>(32);
//    private static List<EffectInstance> s_effectInstances = new List<EffectInstance>(128);
    

    protected override void OnCreateManager()
    {
        base.OnCreateManager();
        
        m_rootGameObject= new GameObject("VFXSystem");
        m_rootGameObject.transform.position = Vector3.zero;
        m_rootGameObject.transform.rotation = Quaternion.identity;
        GameObject.DontDestroyOnLoad(m_rootGameObject);
    }

    protected override void OnDestroyManager()
    {
        base.OnDestroyManager();

        foreach (var effectType in m_EffectTypeData.Values)
        {
            effectType.visualEffect.Reinit();
        }
    }

    
    public void SpawnPointEffect(VisualEffectAsset asset, float3 position, float3 normal)
    {
        m_pointEffectRequests.Add(new PointEffectRequest
        {
            asset = asset,
            position = position,
            normal = normal,
        });
    }
    
    public void SpawnLineEffect(VisualEffectAsset asset, float3 start, float3 end)
    {
        m_lineEffectRequests.Add(new LineEffectRequest
        {
            asset = asset,
            start = start,
            end = end,
        });
    }

//    public static void StopImpact(VisualEffectAsset template)
//    {
//        if (!s_Impacts.ContainsKey(template))
//            RegisterImpactType(template);
//
//        s_Impacts[template].Stop();
//    }

    protected override void OnUpdate()
    {
        // Handle request
        foreach (var request in m_pointEffectRequests)
        {
            EffectTypeData effectType;
            if(!m_EffectTypeData.TryGetValue(request.asset, out effectType))
                effectType = RegisterImpactType(request.asset);
            
//            GameDebug.Log("Spawn effect:" + effectType.visualEffect.name + " pos:" + request.position);

            effectType.eventAttribute.SetVector3(positionID, request.position);
            effectType.eventAttribute.SetVector3(directionID, request.normal);
            effectType.visualEffect.Play(effectType.eventAttribute);
            effectType.visualEffect.pause = false;
            effectType.lastTriggerTime = (float)Game.frameTime;
            effectType.active = true; 
        }
        m_pointEffectRequests.Clear();

        foreach (var request in m_lineEffectRequests)
        {
            EffectTypeData effectType;
            if(!m_EffectTypeData.TryGetValue(request.asset, out effectType))
                effectType = RegisterImpactType(request.asset);

//            GameDebug.Log("Spawn effect:" + effectType.visualEffect.name + " start:" + request.start);

            effectType.eventAttribute.SetVector3(positionID, request.start);
            effectType.eventAttribute.SetVector3(targetPositionID, request.end);
            effectType.visualEffect.Play(effectType.eventAttribute);
            effectType.visualEffect.pause = false;
            effectType.lastTriggerTime = (float)Game.frameTime;
            effectType.active = true;
        }
        m_lineEffectRequests.Clear();
        
        
        
        
        
//        int i = 0;       
//        while (i < s_effectInstances.Count)
//        {
//            if (s_effectInstances[i].endTime >= Game.frameTime)
//            {
//                s_effectInstances.EraseSwap(i);  
//            }
//            else
//            {
//                i++;
//            }            
//        }

        foreach (var effectTypeData in m_EffectTypeData.Values)
        {
            if (effectTypeData.active &&
                (float) Game.frameTime > effectTypeData.lastTriggerTime + effectTypeData.maxDuration)
            {
//                GameDebug.Log("Reinint effect:" + effectTypeData.visualEffect.name);
                effectTypeData.visualEffect.pause = true;
                effectTypeData.active = false;
            }
        }
    }
    
    EffectTypeData RegisterImpactType(VisualEffectAsset template)
    {
        GameDebug.Assert(!m_EffectTypeData.ContainsKey(template));
        GameDebug.Assert(!template != null);
        
        GameObject go = new GameObject(template.name);
        go.transform.parent = m_rootGameObject.transform;
        go.transform.position = Vector3.zero;
        go.transform.rotation = Quaternion.identity;
        go.transform.localScale = Vector3.one;
        var vfx = go.AddComponent<VisualEffect>();
        vfx.visualEffectAsset = template;
        vfx.Reinit();
        vfx.Stop();

        var data = new EffectTypeData
        {
            visualEffect = vfx,
            eventAttribute = vfx.CreateVFXEventAttribute(),
        };
        
        m_EffectTypeData.Add(template, data);

        return data;
    }
}
