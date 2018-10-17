using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;

public class PlayerModuleClient  
{
    public bool PlayerStateReady
    {
        get { return m_LocalPlayer.playerState != null; }
    }

    public bool IsControllingEntity
    {
        get { return PlayerStateReady && m_LocalPlayer.playerState.controlledEntity != Entity.Null; }
    }

    public void GetBufferedCommandsTick(out int firstTick, out int lastTick)
    {
        firstTick = m_LocalPlayer.commandBuffer.FirstTick();
        lastTick = m_LocalPlayer.commandBuffer.LastTick();
    }
    
    public PlayerModuleClient(GameWorld world)
    {
        m_world = world;

        m_HandlePlayerCameraControlSpawn = m_world.GetECSWorld().CreateManager<HandlePlayerCameraControlSpawn>(m_world);
        m_UpdatePlayerCameras = m_world.GetECSWorld().CreateManager<UpdatePlayerCameras>(m_world);
        m_ResolvePlayerReference = m_world.GetECSWorld().CreateManager<ResolvePlayerReference>(m_world);
        m_UpdateServerEntityComponent = m_world.GetECSWorld().CreateManager<UpdateServerEntityComponent>(m_world);

    }

    public void Shutdown()
    {
        m_world.GetECSWorld().DestroyManager(m_HandlePlayerCameraControlSpawn);
        m_world.GetECSWorld().DestroyManager(m_UpdatePlayerCameras);
        m_world.GetECSWorld().DestroyManager(m_ResolvePlayerReference);
        m_world.GetECSWorld().DestroyManager(m_UpdateServerEntityComponent);
        
        if(m_LocalPlayer != null)
            m_world.RequestDespawn(m_LocalPlayer.gameObject);
    }

    public LocalPlayer RegisterLocalPlayer(int playerId, NetworkClient networkClient)
    {
        var prefab = Resources.Load<LocalPlayer>("Prefabs/LocalPlayer");
        m_LocalPlayer = m_world.Spawn<LocalPlayer>(prefab.gameObject);
        m_LocalPlayer.playerId = playerId;
        m_LocalPlayer.networkClient = networkClient;
        m_LocalPlayer.command.lookPitch = 90;
        
        m_ResolvePlayerReference.SetLocalPlayer(m_LocalPlayer);
        return m_LocalPlayer;
    }

    public void SampleInput(bool userInputEnabled, float deltaTime, int renderTick)
    {
        
        // Only sample input when cursor is locked to avoid affecting multiple clients running on same machine (TODO: find better handling of selected window)
        if (userInputEnabled)
            Game.inputSystem.AccumulateInput(ref m_LocalPlayer.command, deltaTime);

        if (m_debugMove.IntValue == 1)
        {
            m_LocalPlayer.command.moveMagnitude = 1;
            m_LocalPlayer.command.lookYaw += 70 * deltaTime;
        }

        if (m_debugMove.IntValue == 2)
        {
            m_debugMoveDuration += deltaTime;

            var fireDuration = 2.0f;
            var jumpDuration = 1.0f;
            var maxTurn = 70.0f;

            if (m_debugMoveDuration > m_debugMovePhaseDuration)
            {
                m_debugMoveDuration = 0;
                m_debugMovePhaseDuration = 4 + 2*Random.value;
                m_debugMoveTurnSpeed = maxTurn *0.9f + Random.value * maxTurn * 0.1f;

                m_debugMoveMag = Random.value > 0.5f ? 1.0f : 0.0f;
            }

            m_LocalPlayer.command.moveMagnitude = m_debugMoveMag;
            m_LocalPlayer.command.lookYaw += m_debugMoveTurnSpeed * deltaTime;
            m_LocalPlayer.command.lookYaw = m_LocalPlayer.command.lookYaw % 360;
            while (m_LocalPlayer.command.lookYaw < 0.0f) m_LocalPlayer.command.lookYaw += 360.0f;
            m_LocalPlayer.command.primaryFire = m_debugMoveDuration < fireDuration;
            m_LocalPlayer.command.jump = m_debugMoveDuration < jumpDuration;
        }
            
        m_LocalPlayer.command.renderTick = renderTick; 
    }

    public void ResetInput(bool userInputEnabled)
    {
        // Clear keys and resample to make sure released keys gets detected.
        // Pass in 0 as deltaTime to make mouse input and view stick do nothing
        Game.inputSystem.ClearInput(ref m_LocalPlayer.command);

        if (userInputEnabled)
            Game.inputSystem.AccumulateInput(ref m_LocalPlayer.command, 0.0f);
    }

    public void HandleCommandReset()
    {
        if (m_LocalPlayer.playerState == null || m_LocalPlayer.playerState.controlledEntity == Entity.Null)
            return;
       
        var commandComponent = m_world.GetEntityManager()
            .GetComponentObject<UserCommandComponent>(m_LocalPlayer.playerState.controlledEntity);
        if (commandComponent.resetCommandTick > commandComponent.lastResetCommandTick)
        {
            commandComponent.lastResetCommandTick = commandComponent.resetCommandTick;

            m_LocalPlayer.command.lookYaw = commandComponent.resetCommandLookYaw;
            m_LocalPlayer.command.lookPitch = commandComponent.resetCommandLookPitch;
        }
    }

    public void ResolveReferenceFromLocalPlayerToPlayer() 
    {
        if (m_LocalPlayer.playerState == null)
            m_ResolvePlayerReference.Update();
    }

    public void HandleControlledEntityChanged()
    {
        m_UpdateServerEntityComponent.Update();
    }
    
    public void StoreCommand(int tick)
    {
        if (m_LocalPlayer.playerState == null)
            return;

        m_LocalPlayer.command.checkTick = tick;

        var lastBufferTick = m_LocalPlayer.commandBuffer.LastTick();
        if (tick != lastBufferTick && tick != lastBufferTick + 1)
        {
            m_LocalPlayer.commandBuffer.Clear();
            GameDebug.Log(string.Format("Trying to store tick:{0} but last buffer tick is:{1}. Clearing buffer", tick, lastBufferTick));
        }
        
        if (tick == lastBufferTick)
            m_LocalPlayer.commandBuffer.Set(ref m_LocalPlayer.command, tick);
        else
            m_LocalPlayer.commandBuffer.Add(ref m_LocalPlayer.command, tick);

#if UNITY_EDITOR        
        StateHistory.SetCommand(tick, ref m_LocalPlayer.command);
#endif
    }

    // Fetches command for a tick and stores it in the UserCommandComponent
    public void RetrieveCommand(int tick)
    {
        GameDebug.Assert(m_LocalPlayer.playerState != null, "No player state set");
        if (m_LocalPlayer.controlledEntity == Entity.Null)
            return;
      
        var userCommand = m_world.GetEntityManager().GetComponentObject<UserCommandComponent>(m_LocalPlayer.controlledEntity);
        GameDebug.Assert(userCommand != null);

        var command = UserCommand.defaultCommand;
        var found = m_LocalPlayer.commandBuffer.TryGetValue(tick, ref command);
        GameDebug.Assert(found, "Failed to find command for tick:{0}",tick);
        
        // Normally we can expect commands to be present, but if client has done hardcatchup commands might not have been generated yet
        // so we just use the defaultCommand
        userCommand.prevCommand = userCommand.command;
        userCommand.command = command;
    }

    public bool HasCommands(int firstTick, int lastTick)
    {
        var hasCommands = m_LocalPlayer.commandBuffer.FirstTick() <= firstTick &&
                          m_LocalPlayer.commandBuffer.LastTick() >= lastTick;
        return hasCommands;
    }

    public void SendCommand(int tick)
    {
        if (m_LocalPlayer.playerState == null)
            return;

        var command =  UserCommand.defaultCommand;
        var commandValid = m_LocalPlayer.commandBuffer.TryGetValue(tick, ref command);        
        if (commandValid)
        {
            m_LocalPlayer.networkClient.QueueCommand(tick, (ref NetworkWriter writer) =>
            {
                command.Serialize(ref writer, null);    
            });
        }
    }

    public void HandleSpawn()
    {
        m_HandlePlayerCameraControlSpawn.Update();
    }
    
    public void CameraUpdate()
    {
        m_UpdatePlayerCameras.Update();
    }

    readonly GameWorld m_world;

    LocalPlayer m_LocalPlayer;        
    
    readonly HandlePlayerCameraControlSpawn m_HandlePlayerCameraControlSpawn;
    readonly UpdatePlayerCameras m_UpdatePlayerCameras;
    readonly ResolvePlayerReference m_ResolvePlayerReference;
    readonly UpdateServerEntityComponent m_UpdateServerEntityComponent;
    
    [ConfigVar(Name = "debugmove", DefaultValue = "0", Description = "Should client perform debug movement")]
    static ConfigVar m_debugMove;
    float m_debugMoveDuration;
    float m_debugMovePhaseDuration;
    float m_debugMoveTurnSpeed;
    float m_debugMoveMag;
}
