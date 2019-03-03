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
        public CharacterPredictedData.Action action;
        public PlayableDirector director;   
    }
    public ActionTimelines[] actionTimelines;

    public Dictionary<CharacterPredictedData.Action, PlayableDirector> m_actionTimelines = new Dictionary<CharacterPredictedData.Action, PlayableDirector>();
    public PlayableDirector m_currentActionTimeline = null;
    public CharacterPredictedData.Action m_prevAction;
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
public class UpdateItemActionTimelineTrigger : BaseComponentSystem<CharacterPresentationSetup, ItemActionTimelineTrigger>
{
    public UpdateItemActionTimelineTrigger(GameWorld world) : base(world) {}
    
    protected override void Update(Entity entity, CharacterPresentationSetup charPresentation, ItemActionTimelineTrigger behavior)
    {
        if (!charPresentation.IsVisible)
            return;
            
        var animState = EntityManager.GetComponentData<CharacterInterpolatedData>(charPresentation.character);
        Update(behavior, animState);
    }
    
    public static void Update(ItemActionTimelineTrigger behavior, CharacterInterpolatedData animState)
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
