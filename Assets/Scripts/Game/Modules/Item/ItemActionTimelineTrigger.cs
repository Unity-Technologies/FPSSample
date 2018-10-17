using System;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using UnityEngine.Experimental.PlayerLoop;
using UnityEngine.Playables;


public class ItemActionTimelineTrigger : MonoBehaviour  
{
    [Serializable]
    public struct ActionTimelines
    {
        public CharacterPredictedState.StateData.Action action;
        public PlayableDirector director;   
    }
    public ActionTimelines[] actionTimelines;

    public Dictionary<CharacterPredictedState.StateData.Action, PlayableDirector> m_actionTimelines = new Dictionary<CharacterPredictedState.StateData.Action, PlayableDirector>();
    public PlayableDirector m_currentActionTimeline = null;
    public CharacterPredictedState.StateData.Action m_prevAction;
    public int m_prevActionTick;

    void Awake()
    {
        foreach (var map in actionTimelines) 
        {
            if(map.director != null)
                m_actionTimelines.Add(map.action, map.director);
        }
            
    }
}

// System
[DisableAutoCreation]
public class UpdateItemActionTimelineTrigger : BaseComponentSystem<CharacterItem, ItemActionTimelineTrigger>
{
    public UpdateItemActionTimelineTrigger(GameWorld world) : base(world) {}
    
    protected override void Update(Entity entity, CharacterItem item, ItemActionTimelineTrigger behavior)
    {
        if (!item.visible)
            return;
            
        var animState = EntityManager.GetComponentData<CharAnimState>(item.character);
        Update(behavior, animState);
    }
    
    public static void Update(ItemActionTimelineTrigger behavior, CharAnimState animState)
    {
        var newAction = behavior.m_prevAction != animState.charAction;
        var newActionTick = behavior.m_prevActionTick != animState.charActionTick;
        if (newAction || newActionTick)
        {
            PlayableDirector director;
            if (behavior.m_actionTimelines.TryGetValue(animState.charAction, out director))
            {
                if (behavior.m_currentActionTimeline != null && director != behavior.m_currentActionTimeline)
                {
                    behavior.m_currentActionTimeline.Stop();
                }

                behavior.m_currentActionTimeline = director;
                behavior.m_currentActionTimeline.time = 0;

                behavior.m_currentActionTimeline.Play();
            }
        }
        behavior.m_prevAction = animState.charAction;
        behavior.m_prevActionTick = animState.charActionTick;
    }
}
