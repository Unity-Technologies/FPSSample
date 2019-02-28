using System;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Experimental.Animations;
using UnityEngine.Playables;
using UnityEngine.Profiling;
using Debug = UnityEngine.Debug;

[CreateAssetMenu(fileName = "Stand", menuName = "FPS Sample/Animation/AnimGraph/Stand")]
public class AnimGraph_Stand : AnimGraphAsset
{
    [ConfigVar(Name = "char.standik", DefaultValue = "1", Description = "Enable stand foot ik")]
    public static ConfigVar useFootIk;

    [ConfigVar(Name = "debug.char.standik", DefaultValue = "0", Description = "Debug foot ik raycast")]
    public static ConfigVar debugStandIk;

    public AnimationClip animIdle;
    public AnimationClip animTurnL;
    public AnimationClip animTurnR;
    public AnimationClip animAimLeft;
    public AnimationClip animAimMid;
    public AnimationClip animAimRight;
    public AnimationClip animShootPose;

    public float animTurnAngle = 90.0f; // Total turn in turn anim
    public float aimTurnLocalThreshold = 90; // Turn threshold
    public float aimYawAngle = 180; // Total yaw in aim animation
    public float turnSpeed = 250;
    public float turnThreshold = 100;
    public float turnTransitionSpeed = 7.5f;

    [Range(0, 2)]
    public float shootPoseMagnitude = 1.0f;
    [Range(0f, 10f)]
    public float shootPoseEnterSpeed = 5f;
    [Range(0f, 10f)]
    public float shootPoseExitSpeed = 5f;
    public AnimationCurve shootPoseEnter;
    public AnimationCurve shootPoseExit;

    [Range(0, 1)]
    public float blendOutAimOnReloadPitch = 0.5f;
    [Range(0, 1)]
    public float blendOutAimOnReloadYaw = 0.5f;
    public AnimationCurve blendOutAimOnReload;

    [Space(10)]
    public FootIkJob.JobSettings footIK;
    public string leftToeBone;
    public string rightToeBone;

    [Space(10)]
    public ActionAnimationDefinition[] actionAnimations;

    public override IAnimGraphInstance Instatiate(EntityManager entityManager, Entity owner, PlayableGraph graph,
        Entity animStateOwner)
    {
        var animState = new CharacterAnimGraph_3PStand(entityManager, owner, graph, animStateOwner, this);
        return animState;
    }

    class CharacterAnimGraph_3PStand : IAnimGraphInstance, IGraphState
    {
        public CharacterAnimGraph_3PStand(EntityManager entityManager, Entity owner, PlayableGraph graph, Entity animStateOwner, AnimGraph_Stand template)
        {            
            if (s_Instances == null)
            {
                s_Instances = new List<CharacterAnimGraph_3PStand>(16);
            }
        
            s_Instances.Add(this);
            
            
            m_template = template;
            m_EntityManager = entityManager;
            m_Owner = owner;
            m_AnimStateOwner = animStateOwner;

            GameDebug.Assert(entityManager.HasComponent<Animator>(owner), "Owner has no Animator component");
            var animator = entityManager.GetComponentObject<Animator>(owner);

            GameDebug.Assert(entityManager.HasComponent<Skeleton>(owner), "Owner has no Skeleton component");
            var skeleton = entityManager.GetComponentObject<Skeleton>(owner);

            GameDebug.Assert(entityManager.HasComponent<CharacterPredictedData>(m_AnimStateOwner),"Owner has no CharPredictedState component");

            var leftToes = skeleton.bones[skeleton.GetBoneIndex(template.leftToeBone.GetHashCode())];
            var rightToes = skeleton.bones[skeleton.GetBoneIndex(template.rightToeBone.GetHashCode())];

            // Locomotion mixer and loco animation
            m_locomotionMixer = AnimationMixerPlayable.Create(graph, (int)LocoMixerPort.Count);

            // Idle
            m_animIdle = AnimationClipPlayable.Create(graph, template.animIdle);
            m_animIdle.SetApplyFootIK(true);
            graph.Connect(m_animIdle, 0, m_locomotionMixer, (int)LocoMixerPort.Idle);
            m_locomotionMixer.SetInputWeight((int)LocoMixerPort.Idle, 1.0f);

            // Turns and trasitions
            m_animTurnL = CreateTurnAnim(graph, template.animTurnL, LocoMixerPort.TurnL);
            m_animTurnR = CreateTurnAnim(graph, template.animTurnR, LocoMixerPort.TurnR);

            var ports = new int[] { (int)LocoMixerPort.Idle, (int)LocoMixerPort.TurnL, (int)LocoMixerPort.TurnR };
            m_Transition = new SimpleTranstion<AnimationMixerPlayable>(m_locomotionMixer, ports);

            // Foot IK  
            if (m_template.animTurnL.events.Length != 0)
            {
                m_LeftTurnFootFalls = ExtractFootFalls(m_template.animTurnL);
                m_RightTurnFootFalls = ExtractFootFalls(m_template.animTurnR);
            }

            var ikJob = new FootIkJob
            {
                settings = m_template.footIK,
                leftToe = animator.BindStreamTransform(leftToes),
                rightToe = animator.BindStreamTransform(rightToes)
            };

            m_footIk = AnimationScriptPlayable.Create(graph, ikJob, 1);
            graph.Connect(m_locomotionMixer, 0, m_footIk, 0);
            m_footIk.SetInputWeight(0, 1f);

            m_defaultLayer = LayerMask.NameToLayer("Default");
            m_playerLayer = LayerMask.NameToLayer("collision_player");
            m_platformLayer = LayerMask.NameToLayer("Platform");

            m_mask = 1 << m_defaultLayer | 1 << m_playerLayer | 1 << m_platformLayer;

            // Aim and Aim mixer
            m_aimMixer = AnimationMixerPlayable.Create(graph, (int)AimMixerPort.Count, true);

            m_animAimLeft = CreateAimAnim(graph, template.animAimLeft, AimMixerPort.AimLeft);
            m_animAimMid = CreateAimAnim(graph, template.animAimMid, AimMixerPort.AimMid);
            m_animAimRight = CreateAimAnim(graph, template.animAimRight, AimMixerPort.AimRight);

            // Setup other additive mixer
            m_additiveMixer = AnimationLayerMixerPlayable.Create(graph);

            var locoMixerPort = m_additiveMixer.AddInput(m_footIk, 0);
            m_additiveMixer.SetInputWeight(locoMixerPort, 1);

            var aimMixerPort = m_additiveMixer.AddInput(m_aimMixer, 0);
            m_additiveMixer.SetInputWeight(aimMixerPort, 1);
            m_additiveMixer.SetLayerAdditive((uint)aimMixerPort, true);

            // Actions
            m_actionAnimationHandler = new ActionAnimationHandler(m_additiveMixer, template.actionAnimations);

            m_ReloadActionAnimation = m_actionAnimationHandler.GetActionAnimation(CharacterPredictedData.Action.Reloading);

            // Shoot pose        
            m_animShootPose = AnimationClipPlayable.Create(graph, template.animShootPose);
            m_animShootPose.SetApplyFootIK(false);
            m_animShootPose.SetDuration(template.animShootPose.length);
            m_animShootPose.Pause();
            m_ShootPosePort = m_additiveMixer.AddInput(m_animShootPose, 0);
            m_additiveMixer.SetInputWeight(m_ShootPosePort, 0.0f);
            m_additiveMixer.SetLayerAdditive((uint)m_ShootPosePort, true);
        }

        public void Shutdown()
        {
            s_Instances.Remove(this);
        }

        public void SetPlayableInput(int index, Playable playable, int playablePort) { }

        public void GetPlayableOutput(int index, ref Playable playable, ref int playablePort)
        {
            playable = m_additiveMixer;
            playablePort = 0;
        }

        public void UpdatePresentationState(bool firstUpdate, GameTime time, float deltaTime)
        {
            Profiler.BeginSample("Stand.Update");

            var animState = m_EntityManager.GetComponentData<CharacterInterpolatedData>(m_AnimStateOwner);
            var predictedState = m_EntityManager.GetComponentData<CharacterPredictedData>(m_AnimStateOwner);

            if (firstUpdate)
            {
                animState.turnDirection = 0;
                animState.turnStartAngle = animState.rotation;
            }

            var aimYawLocal = Mathf.DeltaAngle(animState.rotation, animState.aimYaw);
            var absAimYawLocal = Mathf.Abs(aimYawLocal);

            // Non turning update
            if (animState.turnDirection == 0)
            {
                // Test for local yaw angle exeding threshold so we need to turn
                if (absAimYawLocal > m_template.aimTurnLocalThreshold) // TODO: (sunek) Document why we need local vs non local turn tolerances?
                {
                    var sign = Mathf.Sign(aimYawLocal);
                    animState.turnStartAngle = animState.rotation;
                    animState.turnDirection = (short)sign;
                }
            }

            // Turning update
            float absAngleRemaining = 0f;

            if (animState.turnDirection != 0)
            {
                var rotateAngleRemaining = Mathf.DeltaAngle(animState.rotation, animState.turnStartAngle) + m_template.animTurnAngle * animState.turnDirection;

                if (rotateAngleRemaining * animState.turnDirection <= 0)
                {
                    animState.turnDirection = 0;
                }
                else
                {
                    var turnSpeed = m_template.turnSpeed;
                    if (absAimYawLocal > m_template.turnThreshold)
                    {
                        var factor = 1.0f - (180 - absAimYawLocal) / m_template.turnThreshold;
                        turnSpeed = turnSpeed + factor * 300;
                    }

                    var deltaAngle = deltaTime * turnSpeed;
                    absAngleRemaining = Mathf.Abs(rotateAngleRemaining);
                    if (deltaAngle > absAngleRemaining)
                    {
                        deltaAngle = absAngleRemaining;
                    }

                    var sign = Mathf.Sign(rotateAngleRemaining);

                    animState.rotation += sign * deltaAngle;
                    while (animState.rotation > 360.0f)
                        animState.rotation -= 360.0f;
                    while (animState.rotation < 0.0f)
                        animState.rotation += 360.0f;
                }
            }

            // Shoot pose update   
            if (animState.charAction == CharacterPredictedData.Action.PrimaryFire)
            {
                animState.shootPoseWeight += m_template.shootPoseEnterSpeed * deltaTime;
            }
            else
            {
                animState.shootPoseWeight -= m_template.shootPoseExitSpeed * deltaTime;
            }

            animState.shootPoseWeight = Mathf.Clamp01(animState.shootPoseWeight);

            // Foot IK update
            var footIkJob = m_footIk.GetJobData<FootIkJob>();

            if (m_template.footIK.enabled && useFootIk.IntValue > 0)
            {
                // Figure out stand state
                if (predictedState.velocity.magnitude > 0.001f)
                    m_StandState = StandState.Moving;
                else if (animState.turnDirection != 0 && m_StandState != StandState.TurnStart && m_StandState != StandState.Turning)
                    m_StandState = StandState.TurnStart;
                else if (animState.turnDirection != 0)
                    m_StandState = StandState.Turning;
                else if (animState.turnDirection == 0 && m_StandState == StandState.Turning)
                    m_StandState = StandState.TurnEnd;
                else
                    m_StandState = StandState.Standing;

                // Update foot position
                if (m_StandState == StandState.Moving || firstUpdate)
                {
                    var rotation = Quaternion.Euler(0f, animState.rotation, 0f);
                    m_LeftFootPos = rotation * m_template.footIK.leftToeStandPos + animState.position;
                    m_RightFootPos = rotation * m_template.footIK.rightToeStandPos + animState.position;
                }
                else if (m_StandState == StandState.TurnStart)
                {
                    // Predict foot placement after turn
                    var predictedRotation = Quaternion.Euler(0f, animState.turnStartAngle + m_template.animTurnAngle * animState.turnDirection, 0f);
                    m_LeftFootPos = predictedRotation * m_template.footIK.leftToeStandPos + animState.position;
                    m_RightFootPos = predictedRotation * m_template.footIK.rightToeStandPos + animState.position;
                }

                // Do raycasts
                var rayEmitOffset = Vector3.up * m_template.footIK.emitRayOffset;
                if (m_StandState == StandState.Moving || m_StandState == StandState.TurnStart)
                {
                    var maxRayDistance = m_template.footIK.emitRayOffset + m_template.footIK.maxRayDistance;
                    m_LeftHitSuccess = Physics.Raycast(m_LeftFootPos + rayEmitOffset, Vector3.down, out m_LeftHit, maxRayDistance, m_mask);
                    m_RightHitSuccess = Physics.Raycast(m_RightFootPos + rayEmitOffset, Vector3.down, out m_RightHit, maxRayDistance, m_mask);
                }

                // Update foot offsets
                if (firstUpdate)
                {
                    footIkJob.ikWeight = 0.0f;
                }

                if (m_StandState == StandState.Moving || m_StandState == StandState.TurnEnd)
                {
                    animState.footIkOffset = GetClampedOffset();
                    animState.footIkNormalLeft = m_LeftHit.normal;
                    animState.footIkNormaRight = m_RightHit.normal;

                    m_TurnStartOffset.x = animState.footIkOffset.x;
                    m_TurnStartOffset.y = animState.footIkOffset.y;
                    m_TurnStartNormals[0] = m_LeftHit.normal;
                    m_TurnStartNormals[1] = m_RightHit.normal;
                }

                else if (m_StandState == StandState.TurnStart)
                {
                    m_TurnEndOffset = GetClampedOffset();
                    m_TurnEndNormals[0] = m_LeftHit.normal;
                    m_TurnEndNormals[1] = m_RightHit.normal;
                }

                if (m_StandState == StandState.TurnStart || m_StandState == StandState.Turning)
                {
                    var turnFraction = (-absAngleRemaining + m_template.animTurnAngle) / m_template.animTurnAngle;
                    var footFalls = animState.turnDirection == -1 ? m_LeftTurnFootFalls : m_RightTurnFootFalls;

                    var leftFootFraction = GetFootFraction(turnFraction, footFalls.leftFootUp, footFalls.leftFootDown);
                    animState.footIkOffset.x = Mathf.Lerp(m_TurnStartOffset.x, m_TurnEndOffset.x, leftFootFraction);
                    animState.footIkNormalLeft = Vector3.Lerp(m_TurnStartNormals[0], m_TurnEndNormals[0], leftFootFraction);

                    var rightFootFraction = GetFootFraction(turnFraction, footFalls.rightFootUp, footFalls.rightFootDown);
                    animState.footIkOffset.y = Mathf.Lerp(m_TurnStartOffset.y, m_TurnEndOffset.y, rightFootFraction);
                    animState.footIkNormaRight = Vector3.Lerp(m_TurnStartNormals[1], m_TurnEndNormals[1], rightFootFraction);
                }
            }

#if UNITY_EDITOR
            footIkJob.settings = m_template.footIK;
            DebugSceneView(animState);
#endif
            DebugApplyPresentation();
            
            m_footIk.SetJobData(footIkJob);
            m_EntityManager.SetComponentData(m_AnimStateOwner, animState);
            Profiler.EndSample();
        }

        public void ApplyPresentationState(GameTime time, float deltaTime)
        {
            Profiler.BeginSample("Stand.Apply");

            var animState = m_EntityManager.GetComponentData<CharacterInterpolatedData>(m_AnimStateOwner);

            // Handle turning
            float rotateAngleRemaining = 0f;
            if (animState.turnDirection != 0)
                rotateAngleRemaining = Mathf.DeltaAngle(animState.rotation, animState.turnStartAngle) + m_template.animTurnAngle * animState.turnDirection;

            if (animState.turnDirection == 0)
            {
                m_Transition.Update((int)LocoMixerPort.Idle, m_template.turnTransitionSpeed, Time.deltaTime);
            }
            else
            {
                var fraction = 1f - Mathf.Abs(rotateAngleRemaining / m_template.animTurnAngle);
                var mixerPort = (animState.turnDirection == -1) ? (int)LocoMixerPort.TurnL : (int)LocoMixerPort.TurnR;
                var anim = (animState.turnDirection == -1) ? m_animTurnL : m_animTurnR;

                m_Transition.Update(mixerPort, m_template.turnTransitionSpeed, Time.deltaTime);
                anim.SetTime(anim.GetAnimationClip().length * fraction);

                // Reset the time of the idle, so it's reset when we transition back
                if (m_locomotionMixer.GetInputWeight((int)LocoMixerPort.Idle) < 0.01f)
                    m_animIdle.SetTime(0f);
            }

            // Update aim
            //TODO: Take care of cases where the clip time is 0
            var aimMultiplier = 1f;
            if (animState.charAction == CharacterPredictedData.Action.Reloading && m_template.blendOutAimOnReload != null)
            {
                var normalizedTime = (float)(m_ReloadActionAnimation.animation.GetTime() / m_ReloadActionAnimation.animation.GetDuration());
                aimMultiplier = m_template.blendOutAimOnReload.Evaluate(normalizedTime);
            }

            float aimPitchFraction = animState.aimPitch / 180.0f;
            var aimPitchMult = Mathf.Lerp(m_template.blendOutAimOnReloadPitch, 1f, aimMultiplier);
            aimPitchFraction = Mathf.Lerp(0.5f, aimPitchFraction, aimPitchMult);

            m_animAimLeft.SetTime(aimPitchFraction * m_animAimLeft.GetDuration());
            m_animAimMid.SetTime(aimPitchFraction * m_animAimMid.GetDuration());
            m_animAimRight.SetTime(aimPitchFraction * m_animAimRight.GetDuration());

            float aimYawLocal = Mathf.DeltaAngle(animState.rotation, animState.aimYaw);
            float aimYawFraction = Mathf.Abs(aimYawLocal / m_template.aimYawAngle);
            var aimYawMult = Mathf.Lerp(m_template.blendOutAimOnReloadYaw, 1f, aimMultiplier);
            aimYawFraction = Mathf.Lerp(0.0f, aimYawFraction, aimYawMult);

            m_aimMixer.SetInputWeight((int)AimMixerPort.AimMid, 1.0f - aimYawFraction);
            if (aimYawLocal < 0)
            {
                m_aimMixer.SetInputWeight((int)AimMixerPort.AimLeft, aimYawFraction);
                m_aimMixer.SetInputWeight((int)AimMixerPort.AimRight, 0.0f);
            }
            else
            {
                m_aimMixer.SetInputWeight((int)AimMixerPort.AimLeft, 0.0f);
                m_aimMixer.SetInputWeight((int)AimMixerPort.AimRight, aimYawFraction);
            }

            var characterActionDuration = time.DurationSinceTick(animState.charActionTick);
            m_actionAnimationHandler.UpdateAction(animState.charAction, characterActionDuration);
            m_additiveMixer.SetInputWeight(m_ShootPosePort, m_ShootPoseCurvedWeight * m_template.shootPoseMagnitude);

            // Shoot pose update   
            if (animState.charAction == CharacterPredictedData.Action.PrimaryFire)
            {
                m_ShootPoseCurvedWeight = m_template.shootPoseEnter.Evaluate(animState.shootPoseWeight);
            }
            else
            {
                m_ShootPoseCurvedWeight = m_template.shootPoseExit.Evaluate(animState.shootPoseWeight);
            }

            // Update Foot IK
            var job = m_footIk.GetJobData<FootIkJob>();
            job.normalLeftFoot = animState.footIkNormalLeft;
            job.normalRightFoot = animState.footIkNormaRight;
            job.ikOffset = animState.footIkOffset;
            m_footIk.SetJobData(job);

            Profiler.EndSample();   
            
            DebugUpdatePresentation(animState);
        }

        AnimationClipPlayable CreateAimAnim(PlayableGraph graph, AnimationClip clip, AimMixerPort mixerPort)
        {
            AnimationClipPlayable playable = AnimationClipPlayable.Create(graph, clip);
            playable.SetApplyFootIK(false);
            playable.Pause();
            playable.SetDuration(clip.length);
            graph.Connect(playable, 0, m_aimMixer, (int)mixerPort);
            return playable;
        }

        AnimationClipPlayable CreateTurnAnim(PlayableGraph graph, AnimationClip clip, LocoMixerPort mixerPort)
        {
            AnimationClipPlayable playable = AnimationClipPlayable.Create(graph, clip);
            playable.SetApplyFootIK(true);
            playable.Pause();
            playable.SetDuration(clip.length);

            graph.Connect(playable, 0, m_locomotionMixer, (int)mixerPort);
            m_locomotionMixer.SetInputWeight((int)mixerPort, 0.0f);

            return playable;
        }

        Vector2 GetClampedOffset()
        {
            var leftOffset = 0.0f;
            var rightOffset = 0.0f;

            if (m_LeftHitSuccess)
            {
                leftOffset = Mathf.Clamp(m_LeftHit.point.y - m_LeftFootPos.y + m_template.footIK.leftToeStandPos.y, -m_template.footIK.maxStepSize, m_template.footIK.maxStepSize);
            }

            if (m_RightHitSuccess)
            {
                rightOffset = Mathf.Clamp(m_RightHit.point.y - m_RightFootPos.y + m_template.footIK.rightToeStandPos.y, -m_template.footIK.maxStepSize, m_template.footIK.maxStepSize);
            }

            var stepMag = Mathf.Abs(leftOffset - rightOffset);

            if (stepMag > m_template.footIK.maxStepSize)
            {
                leftOffset = (leftOffset / stepMag) * m_template.footIK.maxStepSize;
                rightOffset = (rightOffset / stepMag) * m_template.footIK.maxStepSize;
            }

            return new Vector2(leftOffset, rightOffset);
        }

        static FootFalls ExtractFootFalls(AnimationClip animation)
        {
            var footFalls = new FootFalls();
            foreach (var e in animation.events)
            {
                if (e.functionName != "OnCharEvent")
                    continue;

                switch (e.stringParameter)
                {
                    case nameof(FootFallType.LeftFootDown):
                        footFalls.leftFootDown = e.time;
                        break;
                    case nameof(FootFallType.LeftFootUp):
                        footFalls.leftFootUp = e.time;
                        break;
                    case nameof(FootFallType.RightFootDown):
                        footFalls.rightFootDown = e.time;
                        break;
                    case nameof(FootFallType.RightFootUp):
                        footFalls.rightFootUp = e.time;
                        break;
                }
            }

            return footFalls;
        }

        static float GetFootFraction(float turnFraction, float footUp, float footDown)
        {
            if (turnFraction <= footUp)
            {
                return 0f;
            }

            if (turnFraction < footDown)
            {
                return (turnFraction - footUp) / (footDown - footUp);
            }

            return 1f;
        }

        void DebugSceneView(CharacterInterpolatedData animState)
        {
            if (m_template.footIK.debugIdlePos)
            {
                var rotation = Quaternion.Euler(0f, animState.rotation, 0f);
                var leftIdlePos = rotation * m_template.footIK.leftToeStandPos + animState.position;
                var rightIdlePos = rotation * m_template.footIK.rightToeStandPos + animState.position;

                DebugDraw.Sphere(leftIdlePos, 0.01f, Color.green);
                DebugDraw.Sphere(leftIdlePos, 0.04f, Color.green);
                DebugDraw.Sphere(rightIdlePos, 0.01f, Color.red);
                DebugDraw.Sphere(rightIdlePos, 0.04f, Color.red);
            }

            if (m_template.footIK.debugRayCast)
            {
                DebugDraw.Sphere(m_LeftFootPos, 0.025f, Color.yellow);
                DebugDraw.Sphere(m_RightFootPos, 0.025f, Color.yellow);

                DebugDraw.Sphere(m_LeftHit.point, 0.015f);
                DebugDraw.Sphere(m_RightHit.point, 0.015f);

                Debug.DrawLine(m_LeftHit.point, m_LeftHit.point + m_LeftHit.normal, Color.green);
                Debug.DrawLine(m_RightHit.point, m_RightHit.point + m_RightHit.normal, Color.red);
            }
        }
        
        void DebugUpdatePresentation(CharacterInterpolatedData animState)
        {
            if (debugStandIk.IntValue > 0)
            {                
                var charIndex = s_Instances.IndexOf(this);
                var lineIndex = charIndex * 3 + 3;
                
                var debugString = "Char " + charIndex + " - IK Offset: " + animState.footIkOffset.x.ToString("0.000") + 
                    ", " + animState.footIkOffset.y.ToString("0.000");
                DebugOverlay.Write(s_DebugColors[charIndex % s_DebugColors.Length], 2, lineIndex, debugString);
                GameDebug.Log(debugString);                                
            }
        }
        
        void DebugApplyPresentation()
        {      
            if (debugStandIk.IntValue > 0)
            {                
                var charIndex = s_Instances.IndexOf(this);
                var lineIndex = charIndex * 3 + 1;

                var color = s_DebugColors[charIndex % s_DebugColors.Length];
                var leftHitString = "Char " + charIndex + " - Left XForm hit:  Nothing";
                if (m_LeftHitSuccess)
                    leftHitString = "Char " + charIndex + " - Left XForm hit:  " + m_LeftHit.transform.name;
                
                DebugOverlay.Write(color, 2, lineIndex, leftHitString);
                GameDebug.Log(leftHitString);

                var rightHitString = "Char " + charIndex + " - Right XForm hit: Nothing";
                if (m_RightHitSuccess)
                    rightHitString = "Char " + charIndex + " - Right XForm hit: " + m_RightHit.transform.name;                
                
                DebugOverlay.Write(color, 2, lineIndex + 1, rightHitString);
                GameDebug.Log(rightHitString);                
            }
        }
        

        enum LocoMixerPort
        {
            Idle,
            TurnL,
            TurnR,
            Count
        }

        enum AimMixerPort
        {
            AimLeft,
            AimMid,
            AimRight,
            Count
        }

        enum FootFallType
        {
            LeftFootUp,
            LeftFootDown,
            RightFootUp,
            RightFootDown
        }

        enum StandState
        {
            Moving,
            Standing,
            Turning,
            TurnStart,
            TurnEnd
        }        

        readonly int m_defaultLayer;
        readonly int m_playerLayer;
        readonly int m_platformLayer;
        readonly int m_mask;

        AnimGraph_Stand m_template;
        EntityManager m_EntityManager;
        Entity m_Owner;
        Entity m_AnimStateOwner;
        AnimationScriptPlayable m_footIk;

        AnimationMixerPlayable m_locomotionMixer;
        AnimationClipPlayable m_animIdle;
        AnimationClipPlayable m_animTurnL;
        AnimationClipPlayable m_animTurnR;

        AnimationMixerPlayable m_aimMixer;
        AnimationClipPlayable m_animAimLeft;
        AnimationClipPlayable m_animAimMid;
        AnimationClipPlayable m_animAimRight;

        AnimationLayerMixerPlayable m_additiveMixer;
        ActionAnimationHandler m_actionAnimationHandler;

        AnimationClipPlayable m_animShootPose;
        ActionAnimationHandler.ActionAnimation m_ReloadActionAnimation;
        int m_ShootPosePort;
        float m_ShootPoseCurvedWeight;

        Vector3 m_LeftFootPos;
        Vector3 m_RightFootPos;

        FootFalls m_LeftTurnFootFalls;
        FootFalls m_RightTurnFootFalls;

        RaycastHit m_LeftHit;
        RaycastHit m_RightHit;
        bool m_LeftHitSuccess;
        bool m_RightHitSuccess;

        Vector2 m_TurnStartOffset;
        Vector2 m_TurnEndOffset;
        Vector3[] m_TurnStartNormals = new Vector3[2];
        Vector3[] m_TurnEndNormals = new Vector3[2];

        StandState m_StandState;

        SimpleTranstion<AnimationMixerPlayable> m_Transition;
    }

    public struct FootFalls
    {
        public float leftFootUp;
        public float leftFootDown;
        public float rightFootUp;
        public float rightFootDown;
    }
    
    // Used for nicely ordered debug logging
    static List<CharacterAnimGraph_3PStand> s_Instances;
    static Color[] s_DebugColors = {Color.blue, Color.red, Color.green};
}
