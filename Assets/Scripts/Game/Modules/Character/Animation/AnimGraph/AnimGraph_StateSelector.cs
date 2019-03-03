using System;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;


[CreateAssetMenu(fileName = "StateSelector", menuName = "FPS Sample/Animation/AnimGraph/StateSelector")]
public class AnimGraph_StateSelector : AnimGraphAsset
{
    // TODO remove this and use character locomotion state
    public enum CharacterAnimationState    
    {
        Stand,
        Run,
        Jump,
        InAir,
        Dead,
        NumStates
    }
   
	[Serializable]
	public struct TransitionDefinition
	{
		public CharacterAnimationState sourceState;
		public float transtionTime;
	}

	[Serializable]
	public struct ControllerDefinition
	{
		public CharacterAnimationState animationState;
	    
		public AnimGraphAsset template;
		[Tooltip("Default transition time from any other state (unless overwritten)")]
		public float transitionTime;
		[Tooltip("Custom transition times from specific states")]
		public TransitionDefinition[] customTransitions;
	}

    public ControllerDefinition[] controllers;
    
	public override IAnimGraphInstance Instatiate(EntityManager entityManager, Entity owner, PlayableGraph graph,
	    Entity animStateOwner)
	{
		return new Instance(entityManager, owner, graph, animStateOwner, this);
	}

    
    class Instance : IAnimGraphInstance, IGraphLogic
    {
        public Instance(EntityManager entityManager, Entity owner, PlayableGraph graph, Entity animStateOwner, AnimGraph_StateSelector settings)
        {
            m_settings = settings;
            m_graph = graph;
            m_EntityManager = entityManager;
            m_Owner = owner;
            m_AnimStateOwner = animStateOwner;
            
            animStateMixer = AnimationMixerPlayable.Create(m_graph, 0, true);
            m_RootPlayable = animStateMixer;


            // Animation states
            animStates = new AnimationControllerEntry[(int)CharacterAnimationState.NumStates];
    
            // Instantiate controllers. We only create one of each type even though it might be used in multiple animation states
            var controllers = new Dictionary<AnimGraphAsset, IAnimGraphInstance>();
            var controllerPorts = new Dictionary<IAnimGraphInstance, int>();
            var stateTransitionPorts = new List<int>();
            var transitionTimes = new Dictionary<IAnimGraphInstance, float[]>();

            foreach (var controllderDef in m_settings.controllers)
            {
                if (controllderDef.template == null)
                    continue;
    
                if (controllers.ContainsKey(controllderDef.template))
                    continue;
    
                var controller = controllderDef.template.Instatiate(entityManager, owner, m_graph, animStateOwner);
                controllers.Add(controllderDef.template, controller);
                
                var outputPlayable = Playable.Null;
                var outputPort = 0;
                controller.GetPlayableOutput(0, ref outputPlayable, ref outputPort);
                var port = animStateMixer.AddInput(outputPlayable, outputPort);
                
                controllerPorts.Add(controller, port);
                stateTransitionPorts.Add(port);
    
                var times = new float[(int)CharacterAnimationState.NumStates];
                for (var i = 0; i < (int)CharacterAnimationState.NumStates; i++)
                {
                    times[i] = controllderDef.transitionTime;
                }
    
                for (var i = 0; i < controllderDef.customTransitions.Length; i++)
                {
                    var sourceStateIndex = (int)controllderDef.customTransitions[i].sourceState;
                    var time = controllderDef.customTransitions[i].transtionTime;
                    times[sourceStateIndex] = time;
                }
    
                transitionTimes.Add(controller, times);
            }
    
            // Setup states specifically defined
            foreach (var controllderDef in m_settings.controllers)
            {
                var animState = controllderDef.animationState;
                if (animStates[(int)animState].controller != null)
                {
                    GameDebug.LogWarning("Animation state already registered");
                    continue;
                }
    
                var controller = controllers[controllderDef.template];
                animStates[(int)animState].controller = controller;
                animStates[(int)animState].animStateUpdater = controller as IGraphState;
                animStates[(int)animState].port = controllerPorts[controller];
                animStates[(int)animState].transitionTimes = transitionTimes[controller];
            }
    
            m_StateTranstion = new SimpleTranstion<AnimationMixerPlayable>(animStateMixer, stateTransitionPorts.ToArray()); 
        }
    
        public void Shutdown()
        {
            for (var i = 0; i < animStates.Length; i++)
            {
                animStates[i].controller.Shutdown();
            }
        }
    
        public void SetPlayableInput(int index, Playable playable, int playablePort)
        {
        }
    
        public void GetPlayableOutput(int index, ref Playable playable, ref int playablePort)
        {
            playable = m_RootPlayable;
            playablePort = 0;
        }
    
        public void UpdateGraphLogic(GameTime time, float deltaTime)
        {
            var animState = GetAnimState();
            var firstUpdate = animState != m_lastAnimState;
            m_lastAnimState = animState;
            
            if(animStates[(int)animState].animStateUpdater != null)
                animStates[(int)animState].animStateUpdater.UpdatePresentationState(firstUpdate, time, deltaTime);
        }

        public void ApplyPresentationState(GameTime time, float deltaTime)
        {
            var animState = GetAnimState();
                        
            // If animation state has changed the new state needs to be started with current state duration to syncronize with server
            var presentationState = m_EntityManager.GetComponentData<CharacterInterpolatedData>(m_AnimStateOwner);
            var charState = m_EntityManager.GetComponentData<CharacterPredictedData>(m_AnimStateOwner);      
            
            if (animState != currentAnimationState || presentationState.charLocoTick != currentAnimationStateTick)
            {
                var previousState = currentAnimationState;
                var prevController = (int)currentAnimationState < animStates.Length ? animStates[(int) previousState].controller : null;
                
                currentAnimationState = animState;
                currentAnimationStateTick = presentationState.charLocoTick;
                var newController =  animStates[(int)currentAnimationState].controller;
                
                // Reset controller and update previous animation state if it has changed
                if (newController != prevController)
                {
                    previousAnimationState = previousAnimationState == CharacterAnimationState.NumStates ? currentAnimationState : previousState;
                }
            }
    
            // Blend to current state
            // We dont replicate blend values for each animAction as we assume just blending to current anim state will be close enough
            var interpolationDuration = animStates[(int)currentAnimationState].transitionTimes[(int)previousAnimationState];
            var blendVel = interpolationDuration > 0 ? 1.0f / interpolationDuration : 1.0f / deltaTime;
            m_StateTranstion.Update(animStates[(int)currentAnimationState].port, blendVel, deltaTime);
    
            // Update any networks that have weight
            for (var i = 0; i < (int)CharacterAnimationState.NumStates; i++)
            {
                if (animStates[i].controller != null  && animStateMixer.GetInputWeight(animStates[i].port) > 0f)
                {
                    animStates[i].controller.ApplyPresentationState(time, deltaTime);
                }
            }
        }
        
        CharacterAnimationState GetAnimState()
        {
            var healthState = m_EntityManager.GetComponentData<HealthStateData>(m_AnimStateOwner);

            if (healthState.health <= 0)
                return CharacterAnimationState.Dead;
                
            var state = m_EntityManager.GetComponentData<CharacterInterpolatedData>(m_AnimStateOwner);
            
            switch (state.charLocoState)
            {
                case CharacterPredictedData.LocoState.GroundMove:
                    return CharacterAnimationState.Run;
                case CharacterPredictedData.LocoState.Jump:
                    return CharacterAnimationState.Jump;
                case CharacterPredictedData.LocoState.DoubleJump:
                    return CharacterAnimationState.InAir;
                case CharacterPredictedData.LocoState.InAir:
                    return CharacterAnimationState.InAir;
            }
    
            return CharacterAnimationState.Stand;
        }

    
        struct AnimationControllerEntry
        {
            public IAnimGraphInstance controller;
            public IGraphState animStateUpdater;
            public int port;
            public float[] transitionTimes;
        }
       
        
        
        AnimGraph_StateSelector m_settings;
        EntityManager m_EntityManager;
        Entity m_Owner;
        Entity m_AnimStateOwner;

        PlayableGraph m_graph;
        
        
        AnimationMixerPlayable m_RootPlayable;
    
        CharacterAnimationState m_lastAnimState = CharacterAnimationState.NumStates;
        CharacterAnimationState currentAnimationState = CharacterAnimationState.NumStates;
        CharacterAnimationState previousAnimationState = CharacterAnimationState.NumStates;
        int currentAnimationStateTick;
    
        AnimationControllerEntry[] animStates;
        AnimationMixerPlayable animStateMixer;
        SimpleTranstion<AnimationMixerPlayable> m_StateTranstion;        
    }	
}
