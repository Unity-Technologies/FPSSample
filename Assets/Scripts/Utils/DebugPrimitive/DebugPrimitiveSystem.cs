using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

[DisableAutoCreation]
public class DestroyPrimsInChannel : BaseComponentSystem
{
    public int channel;
    ComponentGroup Group;
    List<Entity> entityBuffer = new List<Entity>(16);

    public DestroyPrimsInChannel(GameWorld world) : base(world) {}

    protected override void OnCreateManager()
    {
        base.OnCreateManager();
        Group = GetComponentGroup(typeof(DebugPrimitive));
    }

    protected override void OnUpdate()
    {
        var entityArray = Group.GetEntityArray();
        var primArray = Group.GetComponentDataArray<DebugPrimitive>();
        
        entityBuffer.Clear();
        for (int i = 0, c = primArray.Length; i < c; i++)
        {
            var prim = primArray[i];
            if (prim.channel != channel)
                continue;

            GameDebug.Assert(!entityBuffer.Contains(entityArray[i]));
            entityBuffer.Add(entityArray[i]);
        }

        foreach (var entity in entityBuffer)
        {
            var go = EntityManager.GetComponentObject<Transform>(entity).gameObject;
            m_world.RequestDespawn(go, PostUpdateCommands);
        }
    }
}

public class DebugPrimitiveModule
{
    struct SphereRequest
    {
        public int channel;
        public Vector3 center;
        public float radius;
        public Color color;
        public float duration;
    }

    struct LineRequest
    {
        public int channel;
        public Vector3 pA;
        public Vector3 pB;
        public Color color;
        public float duration;
    }
    
    struct CapsuleRequest
    {
        public int channel;
        public Vector3 pA;
        public Vector3 pB;
        public float radius;
        public Color color;
        public float duration;
    }

    public DebugPrimitiveModule(GameWorld world, float colorScale, float heigthOffset)
    {
        m_world = world;
        m_settings = Resources.Load<DebugPrimitiveSystemSettings>("DebugPrimitiveSystemSettings");
        m_colorScale = colorScale;
        m_heigthOffset = new Vector3(0, heigthOffset, 0);
        m_DrawCapsulePrimitives = m_world.GetECSWorld().CreateManager<DrawCapsulePrimitives>();
        m_DrawSpherePrimitive = m_world.GetECSWorld().CreateManager<DrawSpherePrimitives>();
        m_DrawLinePrimitive = m_world.GetECSWorld().CreateManager<DrawLinePrimitives>();
        
        m_DestroyPrimsInChannel = m_world.GetECSWorld().CreateManager<DestroyPrimsInChannel>(m_world); 
    }

    public void Shutdown()
    {
        m_world.GetECSWorld().DestroyManager(m_DrawCapsulePrimitives);
        m_world.GetECSWorld().DestroyManager(m_DrawSpherePrimitive);
        m_world.GetECSWorld().DestroyManager(m_DrawLinePrimitive);
        
        m_world.GetECSWorld().DestroyManager(m_DestroyPrimsInChannel);
    }

    public void DrawPrimitives()
    {
        m_DrawCapsulePrimitives.Update();
        m_DrawSpherePrimitive.Update();
        m_DrawLinePrimitive.Update();
    }

    public static void ClearChannel(int channel)
    {
        var i = 0;
        while (i < m_SphereRequest.Count)
        {
            if (m_SphereRequest[i].channel == channel)
                m_SphereRequest.EraseSwap(i);
            else
                i++;
        }

        i = 0;
        while (i < m_CapsuleRequest.Count)
        {
            if (m_CapsuleRequest[i].channel == channel)
                m_CapsuleRequest.EraseSwap(i);
            else
                i++;
        }  

        i = 0;
        while (i < m_LineRequest.Count)
        {
            if (m_LineRequest[i].channel == channel)
                m_LineRequest.EraseSwap(i);
            else
                i++;
        }  

        
        if (m_PendingChannelClear.Contains(channel))
            return;
        
        m_PendingChannelClear.Add(channel);
    }

    public static void CreateSpherePrimitive(int channel, Vector3 center, float radius, Color color, float duration)
    {
        m_SphereRequest.Add(new SphereRequest()
        {
            channel = channel,
            center = center,
            radius = radius,
            color = color,
            duration = duration
        });
    }
    
    public static void CreateCapsulePrimitive(int channel, Vector3 pA, Vector3 pB, float radius, Color color, float duration)
    {
        m_CapsuleRequest.Add(new CapsuleRequest()
        {
            channel = channel,
            pA = pA,
            pB = pB,
            radius = radius,
            color = color,
        });
    }

    public static void CreateLinePrimitive(int channel, Vector3 pA, Vector3 pB, Color color, float duration)
    {
        m_LineRequest.Add(new LineRequest()
        {
            channel = channel,
            pA = pA,
            pB = pB,
            color = color,
        });
    }

    public void HandleRequests()
    {
        foreach (var channel in m_PendingChannelClear)
        {
            m_DestroyPrimsInChannel.channel = channel;
            m_DestroyPrimsInChannel.Update();
        }
        m_PendingChannelClear.Clear();
            
        foreach (var request in m_SphereRequest)
        {
            var prim = m_world.Spawn<SpherePrimitive>(m_settings.spherePrefab.gameObject);
            prim.center = request.center + m_heigthOffset;
            prim.radius = request.radius;
            prim.color = request.color*m_colorScale;
            var entity = prim.gameObject.GetComponent<GameObjectEntity>().Entity;
            m_world.GetEntityManager().AddComponentData(entity, new DebugPrimitive() { channel = request.channel });
        }
        m_SphereRequest.Clear();
        
        foreach (var request in m_CapsuleRequest)
        {
            var prim = m_world.Spawn<CapsulePrimitive>(m_settings.capsulePrefab.gameObject);
            prim.pA = request.pA + m_heigthOffset;
            prim.pB = request.pB + m_heigthOffset;
            prim.radius = request.radius;
            prim.color = request.color*m_colorScale;
            var entity = prim.gameObject.GetComponent<GameObjectEntity>().Entity;
            m_world.GetEntityManager().AddComponentData(entity, new DebugPrimitive() { channel = request.channel });
        }
        m_CapsuleRequest.Clear();

        foreach (var request in m_LineRequest)
        {
            var prim = m_world.Spawn<LinePrimitive>(m_settings.linePrefab.gameObject);
            prim.pA = request.pA + m_heigthOffset;
            prim.pB = request.pB + m_heigthOffset;
            prim.color = request.color*m_colorScale;
            var entity = prim.gameObject.GetComponent<GameObjectEntity>().Entity;
            m_world.GetEntityManager().AddComponentData(entity, new DebugPrimitive() { channel = request.channel });
        }
        m_LineRequest.Clear();
    }
    
    static GameWorld m_world;
    static DebugPrimitiveSystemSettings m_settings;  

    static float m_colorScale;
    static Vector3 m_heigthOffset;

    private static List<SphereRequest> m_SphereRequest = new List<SphereRequest>(32);
    private static List<CapsuleRequest> m_CapsuleRequest = new List<CapsuleRequest>(32);
    private static List<LineRequest> m_LineRequest = new List<LineRequest>(32);
    private static List<int> m_PendingChannelClear = new List<int>(32);
    
    DrawCapsulePrimitives m_DrawCapsulePrimitives;
    DrawSpherePrimitives m_DrawSpherePrimitive;
    DrawLinePrimitives m_DrawLinePrimitive;

    DestroyPrimsInChannel m_DestroyPrimsInChannel;
}
