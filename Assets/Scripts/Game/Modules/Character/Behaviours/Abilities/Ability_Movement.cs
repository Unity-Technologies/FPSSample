using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

[CreateAssetMenu(fileName = "Ability_Movement",menuName = "FPS Sample/Abilities/Ability_Movement")]
public class Ability_Movement : CharBehaviorFactory
{
    public struct Settings : IComponentData
    {
        public float UNUSED_moveSpeed;
    }

    public Settings settings;
    
    public override Entity Create(EntityManager entityManager, List<Entity> entities)
    {
        var entity = CreateCharBehavior(entityManager);
        entities.Add(entity);
		
        // Ability components
        entityManager.AddComponentData(entity, settings);

        return entity;
    }
}


[DisableAutoCreation]
class Movement_RequestActive : BaseComponentDataSystem<CharBehaviour,AbilityControl,Ability_Movement.Settings>
{
    public Movement_RequestActive(GameWorld world) : base(world)
    {
        ExtraComponentRequirements = new ComponentType[] { typeof(ServerEntity) } ;
    }

    protected override void Update(Entity entity, CharBehaviour charAbility, AbilityControl abilityCtrl,
        Ability_Movement.Settings settings)
    {
        if (abilityCtrl.behaviorState == AbilityControl.State.Active || abilityCtrl.behaviorState == AbilityControl.State.Cooldown)
            return;

        if (abilityCtrl.active == 0 && abilityCtrl.behaviorState != AbilityControl.State.RequestActive)
        {
            abilityCtrl.behaviorState = AbilityControl.State.RequestActive;
            EntityManager.SetComponentData(entity, abilityCtrl);
        }
    }
}


[DisableAutoCreation]
class Movement_Update : BaseComponentDataSystem<CharBehaviour, AbilityControl, Ability_Movement.Settings>
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
    
    protected override void Update(Entity abilityEntity, CharBehaviour charAbility, AbilityControl abilityCtrl, Ability_Movement.Settings settings )
    {
        if (abilityCtrl.active == 0)
        {
            if (abilityCtrl.behaviorState != AbilityControl.State.Idle)
            {
                abilityCtrl.behaviorState = AbilityControl.State.Idle;
                EntityManager.SetComponentData(abilityEntity, abilityCtrl);
            }
            return;
        }

        // Movement is always active (unless canceled)
        abilityCtrl.behaviorState = AbilityControl.State.Active;
        EntityManager.SetComponentData(abilityEntity, abilityCtrl);
        
        
        var time = m_world.worldTime;
       
        var command = EntityManager.GetComponentData<UserCommandComponentData>(charAbility.character).command;
        var predictedState = EntityManager.GetComponentData<CharacterPredictedData>(charAbility.character);
        var character = EntityManager.GetComponentObject<Character>(charAbility.character);
        
        var newPhase = CharacterPredictedData.LocoState.MaxValue;
        
        var phaseDuration = time.DurationSinceTick(predictedState.locoStartTick);

        var isOnGround = predictedState.IsOnGround();
        var isMoveWanted = command.moveMagnitude != 0.0f;

        // Ground movement
        if (isOnGround)
        {
            if (isMoveWanted)
            {
                newPhase = CharacterPredictedData.LocoState.GroundMove;
            }
            else
            {
                newPhase = CharacterPredictedData.LocoState.Stand;
            }
        }
        
        // Jump
        if (isOnGround)
            predictedState.jumpCount = 0;

        if (command.buttons.IsSet(UserCommand.Button.Jump) && isOnGround)
        {
            predictedState.jumpCount = 1;
            newPhase = CharacterPredictedData.LocoState.Jump;
        }

        if (command.buttons.IsSet(UserCommand.Button.Jump) && predictedState.locoState == CharacterPredictedData.LocoState.InAir && predictedState.jumpCount < 2)
        {
            predictedState.jumpCount = predictedState.jumpCount + 1;
            predictedState.velocity.y = 0;
            newPhase = CharacterPredictedData.LocoState.DoubleJump;
        }

        if (predictedState.locoState == CharacterPredictedData.LocoState.Jump || predictedState.locoState == CharacterPredictedData.LocoState.DoubleJump)
        {
            if (phaseDuration >= Game.config.jumpAscentDuration)
            {
                newPhase = CharacterPredictedData.LocoState.InAir;
            }
        }

        // Set phase start tick if phase has changed
        if (newPhase != CharacterPredictedData.LocoState.MaxValue && newPhase != predictedState.locoState)
        {
            predictedState.locoState = newPhase;
            predictedState.locoStartTick = time.tick;
        }
        
        if (debugCharacterMove.IntValue > 0)
        {
            // Only show for one player
            if (lastUsedFrame < Time.frameCount)
            {
                lastUsedFrame = Time.frameCount;

                int o = Time.frameCount % movehist_x.Length;
                movehist_x[o] = predictedState.position.x % 10.0f;
                movehist_y[o] = predictedState.position.y % 10.0f;
                movehist_z[o] = predictedState.position.z % 10.0f;

                DebugOverlay.DrawGraph(4, 4, 10, 5, movehist_x, o, Color.red, 10.0f);
                DebugOverlay.DrawGraph(4, 12, 10, 5, movehist_y, o, Color.green, 10.0f);
                DebugOverlay.DrawGraph(4, 20, 10, 5, movehist_z, o, Color.blue, 10.0f);
            }
        }

        if (time.tick != predictedState.tick + 1)
            GameDebug.Log("Update tick invalid. Game tick:" + time.tick + " but current state is at tick:" + predictedState.tick);

        predictedState.tick = time.tick;

        // Apply damange impulse from previus frame
        if (time.tick == predictedState.damageTick + 1)
        {
            predictedState.velocity += predictedState.damageDirection*predictedState.damageImpulse;
            predictedState.locoState = CharacterPredictedData.LocoState.InAir;
            predictedState.locoStartTick = time.tick;
        }
        
        var moveQuery = EntityManager.GetComponentObject<CharacterMoveQuery>(charAbility.character);

        // Simple adjust of height while on platform
        if (predictedState.locoState == CharacterPredictedData.LocoState.Stand && 
            character.groundCollider != null && 
            character.groundCollider.gameObject.layer == m_platformLayer)
        {
            if (character.altitude < moveQuery.settings.skinWidth - 0.01f )
            {
                var platform = character.groundCollider;
                var posY = platform.transform.position.y + moveQuery.settings.skinWidth;
                predictedState.position.y = posY;
            }
        }

        // Calculate movement and move character
        var deltaPos = Vector3.zero;
        CalculateMovement(ref time, ref predictedState, ref command, ref deltaPos);

        // Setup movement query
        moveQuery.collisionLayer = character.teamId == 0 ? m_charCollisionALayer : m_charCollisionBLayer;
        moveQuery.moveQueryStart = predictedState.position;
        moveQuery.moveQueryEnd = moveQuery.moveQueryStart + (float3)deltaPos;
        
        EntityManager.SetComponentData(charAbility.character,predictedState);
    }
    
    void CalculateMovement(ref GameTime gameTime, ref CharacterPredictedData predicted, ref UserCommand command, ref Vector3 deltaPos)
    {
        var velocity = predicted.velocity;
        switch (predicted.locoState)
        {
            case CharacterPredictedData.LocoState.Jump:
            case CharacterPredictedData.LocoState.DoubleJump:

                // In jump we overwrite velocity y component with linear movement up
                velocity = CalculateGroundVelocity(velocity, ref command, Game.config.playerSpeed, Game.config.playerAirFriction, Game.config.playerAiracceleration, gameTime.tickDuration);
                velocity.y = Game.config.jumpAscentHeight / Game.config.jumpAscentDuration;
                deltaPos += velocity * gameTime.tickDuration;

                return;
            case CharacterPredictedData.LocoState.InAir:

                var gravity = Game.config.playerGravity;
                velocity += Vector3.down * gravity * gameTime.tickDuration;
                velocity = CalculateGroundVelocity(velocity, ref command, Game.config.playerSpeed, Game.config.playerAirFriction, Game.config.playerAiracceleration, gameTime.tickDuration);

                if (velocity.y < -Game.config.maxFallVelocity)
                    velocity.y = -Game.config.maxFallVelocity;

                // Cheat movement
                if (command.buttons.IsSet(UserCommand.Button.Boost) && (Game.GetGameLoop<PreviewGameLoop>() != null))
                {
                    velocity.y += 25.0f * gameTime.tickDuration;
                    velocity.y = Mathf.Clamp(velocity.y, -2.0f, 10.0f);
                }

                deltaPos = velocity * gameTime.tickDuration;

                return;
        }

        var playerSpeed = predicted.sprinting == 1 ? Game.config.playerSprintSpeed : Game.config.playerSpeed;

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
        var wantedSpeed = playerSpeed * command.moveMagnitude;
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
class Movement_HandleCollision : BaseComponentDataSystem<CharBehaviour, AbilityControl, Ability_Movement.Settings>
{
    public Movement_HandleCollision(GameWorld world) : base(world)
    {
        ExtraComponentRequirements = new ComponentType[] { typeof(ServerEntity) } ;
    }
    
    protected override void Update(Entity abilityEntity, CharBehaviour charAbility, AbilityControl abilityCtrl, Ability_Movement.Settings settings)
    {
        if (abilityCtrl.active == 0)
            return;

        
        var time = m_world.worldTime;
        var predictedState = EntityManager.GetComponentData<CharacterPredictedData>(charAbility.character);
        var query = EntityManager.GetComponentObject<CharacterMoveQuery>(charAbility.character);
        var command = EntityManager.GetComponentData<UserCommandComponentData>(charAbility.character).command;

        // Check for ground change (hitting ground or leaving ground)  
        var isOnGround = predictedState.IsOnGround();
        if (isOnGround != query.isGrounded)
        {
            if (query.isGrounded)
            {
                if (command.moveMagnitude != 0.0f)
                {
                    predictedState.locoState = CharacterPredictedData.LocoState.GroundMove;  
                }
                else
                {
                    predictedState.locoState = CharacterPredictedData.LocoState.Stand;    
                }
            }
            else
            {
                predictedState.locoState = CharacterPredictedData.LocoState.InAir;                    
            }
            
            predictedState.locoStartTick = time.tick;
        }
    
        // Manually calculate resulting velocity as characterController.velocity is linked to Time.deltaTime
        var newPos = query.moveQueryResult;
        var oldPos = query.moveQueryStart;
        var velocity = (newPos - oldPos) / time.tickDuration;
    
        predictedState.velocity = velocity;
        predictedState.position = query.moveQueryResult;
        
        EntityManager.SetComponentData(charAbility.character, predictedState);
    }
}
