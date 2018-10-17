using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Profiling;

[RequireComponent(typeof(ReplicatedAbility))]
public class Ability_Movement : MonoBehaviour
{
    public struct Settings : IComponentData
    {
        public float UNUSED_moveSpeed;
    }

    public Settings settings;

    private void OnEnable()
    {
        var gameObjectEntity = GetComponent<GameObjectEntity>();
        var entityManager = gameObjectEntity.EntityManager;
        var abilityEntity = gameObjectEntity.Entity;
        
        // Default components
        entityManager.AddComponentData(abilityEntity, new CharacterAbility());
        entityManager.AddComponentData(abilityEntity, new AbilityControl());

        // Ability components
        entityManager.AddComponentData(abilityEntity, settings);

        // Setup replicated ability        
        var replicatedAbility = entityManager.GetComponentObject<ReplicatedAbility>(abilityEntity);
        replicatedAbility.predictedHandlers = new IPredictedDataHandler[1];
        replicatedAbility.predictedHandlers[0] = new PredictedEntityHandler<AbilityControl>(entityManager, abilityEntity);
    }
}


[DisableAutoCreation]
class Movement_Update : BaseComponentDataSystem<CharacterAbility, Ability_Movement.Settings>
{
    [ConfigVar(Name = "debug.charactermove", Description = "Show graphs of one character's movement along x, y, z", DefaultValue = "0")]
    public static ConfigVar debugCharacterMove;
    
    // Debugging graphs to show player movement in 3 axis
    static float[] movehist_x = new float[100];
    static float[] movehist_y = new float[100];
    static float[] movehist_z = new float[100];
    static float lastUsedFrame;

    readonly int m_platformLayer;
    readonly int m_charCollisionALayer; 
    readonly int m_charCollisionBLayer; 

    public Movement_Update(GameWorld world) : base(world)
    {
        m_platformLayer = LayerMask.NameToLayer("Platform");
        m_charCollisionALayer = LayerMask.NameToLayer("CharCollisionA");
        m_charCollisionBLayer = LayerMask.NameToLayer("CharCollisionB");
        ExtraComponentRequirements = new ComponentType[] { typeof(ServerEntity) } ;
    }
    
    protected override void Update(Entity abilityEntity, CharacterAbility charAbility, Ability_Movement.Settings settings )
    {
        Profiler.BeginSample("Movement_Update");
        
        var time = m_world.worldTime;
       
        var command = EntityManager.GetComponentObject<UserCommandComponent>(charAbility.character).command;
        var characterPredictedState = EntityManager.GetComponentObject<CharacterPredictedState>(charAbility.character);
        var health = EntityManager.GetComponentObject<HealthState>(charAbility.character);

        var newPhase = CharacterPredictedState.StateData.LocoState.MaxValue;
        
        var phaseDuration = time.DurationSinceTick(characterPredictedState.State.locoStartTick);

        var isOnGround = characterPredictedState.State.IsOnGround();
        var isMoveWanted = command.moveMagnitude != 0.0f;

        if (health.health <= 0)
        {
            newPhase = CharacterPredictedState.StateData.LocoState.Dead;
        }
        else
        {
            // Ground movement
            if (isOnGround)
            {
                if (isMoveWanted)
                {
                    newPhase = CharacterPredictedState.StateData.LocoState.GroundMove;
                }
                else
                {
                    newPhase = CharacterPredictedState.StateData.LocoState.Stand;
                }
            }
            
            // Jump
            if (isOnGround)
                characterPredictedState.State.jumpCount = 0;

            if (command.jump && isOnGround)
            {
                characterPredictedState.State.jumpCount = 1;
                newPhase = CharacterPredictedState.StateData.LocoState.Jump;
            }

            if (command.jump && characterPredictedState.State.locoState == CharacterPredictedState.StateData.LocoState.InAir && characterPredictedState.State.jumpCount < 2)
            {
                characterPredictedState.State.jumpCount = characterPredictedState.State.jumpCount + 1;
                characterPredictedState.State.velocity.y = 0;
                newPhase = CharacterPredictedState.StateData.LocoState.DoubleJump;
            }

            if (characterPredictedState.State.locoState == CharacterPredictedState.StateData.LocoState.Jump || characterPredictedState.State.locoState == CharacterPredictedState.StateData.LocoState.DoubleJump)
            {
                if (phaseDuration >= Game.config.jumpAscentDuration)
                {
                    newPhase = CharacterPredictedState.StateData.LocoState.InAir;
                }
            }
        }

        // Set phase start tick if phase has changed
        if (newPhase != CharacterPredictedState.StateData.LocoState.MaxValue && newPhase != characterPredictedState.State.locoState)
        {
            characterPredictedState.State.locoState = newPhase;
            characterPredictedState.State.locoStartTick = time.tick;
        }
        
        if (debugCharacterMove.IntValue > 0)
        {
            // Only show for one player
            if (lastUsedFrame < Time.frameCount)
            {
                lastUsedFrame = Time.frameCount;

                int o = Time.frameCount % movehist_x.Length;
                movehist_x[o] = characterPredictedState.State.position.x % 10.0f;
                movehist_y[o] = characterPredictedState.State.position.y % 10.0f;
                movehist_z[o] = characterPredictedState.State.position.z % 10.0f;

                DebugOverlay.DrawGraph(4, 4, 10, 5, movehist_x, o, Color.red, 10.0f);
                DebugOverlay.DrawGraph(4, 12, 10, 5, movehist_y, o, Color.green, 10.0f);
                DebugOverlay.DrawGraph(4, 20, 10, 5, movehist_z, o, Color.blue, 10.0f);
            }
        }

        if (time.tick != characterPredictedState.State.tick + 1)
            GameDebug.LogError("Update tick invalid. Game tick:" + time.tick + " but current state is at tick:" + characterPredictedState.State.tick);

        characterPredictedState.State.tick = time.tick;

        // Apply damange impulse from previus frame
        if (time.tick == characterPredictedState.State.damageTick + 1)
        {
            characterPredictedState.State.velocity += characterPredictedState.State.damageDirection*characterPredictedState.State.damageImpulse;
            characterPredictedState.State.locoState = CharacterPredictedState.StateData.LocoState.InAir;
            characterPredictedState.State.locoStartTick = time.tick;
        }
        
        var moveQuery = EntityManager.GetComponentObject<CharacterMoveQuery>(charAbility.character);

        // Simple adjust of height while on platform
        if (characterPredictedState.State.locoState == CharacterPredictedState.StateData.LocoState.Stand && 
            characterPredictedState.groundCollider != null && 
            characterPredictedState.groundCollider.gameObject.layer == m_platformLayer)
        {
            if (characterPredictedState.altitude < moveQuery.settings.skinWidth - 0.01f )
            {
                var platform = characterPredictedState.groundCollider;
                var posY = platform.transform.position.y + moveQuery.settings.skinWidth;
                characterPredictedState.State.position.y = posY;
            }
        }

        // Calculate movement and move character
        var deltaPos = Vector3.zero;
        CalculateMovement(ref time, characterPredictedState, ref command, ref deltaPos);

        // Setup movement query
        moveQuery.collisionLayer = characterPredictedState.teamId == 0 ? m_charCollisionALayer : m_charCollisionBLayer;
        moveQuery.moveQueryStart = characterPredictedState.State.position;
        moveQuery.moveQueryEnd = moveQuery.moveQueryStart + (float3)deltaPos; 
        
        Profiler.EndSample();
    }
    
    void CalculateMovement(ref GameTime gameTime, CharacterPredictedState predictedState, ref UserCommand command, ref Vector3 deltaPos)
    {
        var velocity = predictedState.State.velocity;
        switch (predictedState.State.locoState)
        {
            case CharacterPredictedState.StateData.LocoState.Jump:
            case CharacterPredictedState.StateData.LocoState.DoubleJump:

                // In jump we overwrite velocity y component with linear movement up
                velocity = CalculateGroundVelocity(velocity, ref command, Game.config.playerSpeed, Game.config.playerAirFriction, Game.config.playerAiracceleration, gameTime.tickDuration);
                velocity.y = Game.config.jumpAscentHeight / Game.config.jumpAscentDuration;
                deltaPos += velocity * gameTime.tickDuration;

                return;
            case CharacterPredictedState.StateData.LocoState.InAir:

                var gravity = Game.config.playerGravity;
                velocity += Vector3.down * gravity * gameTime.tickDuration;
                velocity = CalculateGroundVelocity(velocity, ref command, Game.config.playerSpeed, Game.config.playerAirFriction, Game.config.playerAiracceleration, gameTime.tickDuration);

                if (velocity.y < -Game.config.maxFallVelocity)
                    velocity.y = -Game.config.maxFallVelocity;

                // Cheat movement
                if (command.boost && (Game.GetGameLoop<PreviewGameLoop>() != null))
                {
                    velocity.y += 25.0f * gameTime.tickDuration;
                    velocity.y = Mathf.Clamp(velocity.y, -2.0f, 10.0f);
                }

                deltaPos = velocity * gameTime.tickDuration;

                return;
            case CharacterPredictedState.StateData.LocoState.Dead:
                deltaPos = Vector3.zero;
                return;
        }

        var playerSpeed = predictedState.State.sprinting ? Game.config.playerSprintSpeed : Game.config.playerSpeed;

        velocity = CalculateGroundVelocity(velocity, ref command, playerSpeed, Game.config.playerFriction, Game.config.playerAcceleration, gameTime.tickDuration);
//        Debug.DrawLine(predictedState.State.position, predictedState.State.position + velocity, Color.yellow,1 );
        
        // Simple follow ground code so character sticks to ground when running down hill
        velocity.y = -400.0f*gameTime.tickDuration;
        
//        Debug.DrawLine(predictedState.State.position, predictedState.State.position + velocity, Color.green, 1 );
        
        deltaPos = velocity * gameTime.tickDuration;
    }

    Vector3 CalculateGroundVelocity(Vector3 velocity, ref UserCommand command, float playerSpeed, float friction, float acceleration, float deltaTime)
    {
        var moveYawRotation = Quaternion.Euler(0, command.lookYaw + command.moveYaw, 0);
        var moveVec = moveYawRotation * Vector3.forward * command.moveMagnitude;

        // Applying friction
        var groundVelocity = new Vector3(velocity.x, 0, velocity.z);
        var groundSpeed = groundVelocity.magnitude;
        var frictionSpeed = Mathf.Max(groundSpeed, 1.0f) * deltaTime * friction;
        var newGroundSpeed = groundSpeed - frictionSpeed;
        if (newGroundSpeed < 0)
            newGroundSpeed = 0;
        if (groundSpeed > 0)
            groundVelocity *= (newGroundSpeed / groundSpeed);

        // Doing actual movement (q2 style)
        var wantedGroundVelocity = moveVec * playerSpeed;
        var wantedGroundDir = wantedGroundVelocity.normalized;
        var currentSpeed = Vector3.Dot(wantedGroundDir, groundVelocity);
        var wantedSpeed = playerSpeed;
        var deltaSpeed = wantedSpeed - currentSpeed;
        if (deltaSpeed > 0.0f)
        {
            var accel = deltaTime * acceleration * playerSpeed;
            var speed_adjustment = Mathf.Clamp(accel, 0.0f, deltaSpeed) * wantedGroundDir;
            groundVelocity += speed_adjustment;
        }

        if (!Game.config.easterBunny)
        {
            newGroundSpeed = groundVelocity.magnitude;
            if (newGroundSpeed > playerSpeed)
                groundVelocity *= playerSpeed / newGroundSpeed;
        }

        velocity.x = groundVelocity.x;
        velocity.z = groundVelocity.z;

        return velocity;
    }
}

[DisableAutoCreation]
class Movement_HandleCollision : BaseComponentDataSystem<CharacterAbility>
{
    public Movement_HandleCollision(GameWorld world) : base(world)
    {
        ExtraComponentRequirements = new ComponentType[] { typeof(ServerEntity) } ;
    }
    
    protected override void Update(Entity abilityEntity, CharacterAbility charAbility)
    {
        Profiler.BeginSample("Movement_HandleCollision");
        
        
        
        
        
        var time = m_world.worldTime;
        var character = EntityManager.GetComponentObject<CharacterPredictedState>(charAbility.character);
        var query = EntityManager.GetComponentObject<CharacterMoveQuery>(charAbility.character);
        var command = EntityManager.GetComponentObject<UserCommandComponent>(charAbility.character).command;
        
        // Check for ground change (hitting ground or leaving ground)  
        if (character.State.locoState != CharacterPredictedState.StateData.LocoState.Dead)
        {
            var isOnGround = character.State.IsOnGround();
            if (isOnGround != query.isGrounded)
            {
                if (query.isGrounded)
                {
                    if (command.moveMagnitude != 0.0f)
                    {
                        character.State.locoState = CharacterPredictedState.StateData.LocoState.GroundMove;  
                    }
                    else
                    {
                        character.State.locoState = CharacterPredictedState.StateData.LocoState.Stand;    
                    }
                }
                else
                {
                    character.State.locoState = CharacterPredictedState.StateData.LocoState.InAir;                    
                }
                
                character.State.locoStartTick = time.tick;
            }
        }
    
        // Manually calculate resulting velocity as characterController.velocity is linked to Time.deltaTime
        var newPos = query.moveQueryResult;
        var oldPos = query.moveQueryStart;
        var velocity = (newPos - oldPos) / time.tickDuration;
    
        character.State.velocity = velocity;
        character.State.position = query.moveQueryResult;
        
        Profiler.EndSample();
    }
}
