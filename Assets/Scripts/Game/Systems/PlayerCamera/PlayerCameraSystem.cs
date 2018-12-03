using UnityEngine;
using Unity.Entities;
using System.Collections.Generic;

[DisableAutoCreation]
public class HandlePlayerCameraControlSpawn : InitializeComponentSystem<PlayerCameraSettings>
{
    public HandlePlayerCameraControlSpawn(GameWorld world) : base(world)
    {
        m_cameraPrefab = Resources.Load<PlayerCamera>("Prefabs/PlayerCamera");
    }

    protected override void Initialize(Entity entity, PlayerCameraSettings component)
    {
        var camera = m_world.Spawn<PlayerCamera>(m_cameraPrefab.gameObject);
        camera.cameraSettings = component;
        camera.gameObject.SetActive(false);
    }

    PlayerCamera m_cameraPrefab;
}

[DisableAutoCreation]
public class UpdatePlayerCameras : BaseComponentSystem
{
    public ComponentGroup Group;

    public UpdatePlayerCameras(GameWorld world) : base(world) { }

    protected override void OnCreateManager()
    {
        base.OnCreateManager();
        Group = GetComponentGroup(typeof(PlayerCamera), typeof(Camera));
    }

    protected override void OnUpdate()
    {
        var cameraArray = Group.GetComponentArray<Camera>();
        var playerCameraArray = Group.GetComponentArray<PlayerCamera>();
        for (var i = 0; i < cameraArray.Length; i++)
        {
            var camera = cameraArray[i];
            var playerCamera = playerCameraArray[i];
            var settings = playerCamera.cameraSettings;
            var enabled = settings.isEnabled;
            var isActive = camera.gameObject.activeSelf;
            if (!enabled)
            {
                if (isActive)
                {
                    Game.game.PopCamera(camera);
                    camera.gameObject.SetActive(false);
                }
                continue;
            }

            if (!isActive)
            {
                camera.gameObject.SetActive(true);
                Game.game.PushCamera(camera);
            }

            camera.fieldOfView = settings.fieldOfView;
            if (debugCameraDetach.IntValue == 0)
            {
                // Normal movement
                camera.transform.position = settings.position;
                camera.transform.rotation = settings.rotation;
            }
            else if(debugCameraDetach.IntValue == 1)
            {
                // Move char but still camera
            }


            if(debugCameraDetach.ChangeCheck())
            {
                // Block normal input
                Game.Input.SetBlock(Game.Input.Blocker.Debug, debugCameraDetach.IntValue == 2);
            }
            if (debugCameraDetach.IntValue == 2 && !Console.IsOpen())
            {
                var eu = camera.transform.localEulerAngles;
                if (eu.x > 180.0f) eu.x -= 360.0f;
                eu.x = Mathf.Clamp(eu.x, -70.0f, 70.0f);
                eu += new Vector3(-Input.GetAxisRaw("Mouse Y"), Input.GetAxisRaw("Mouse X"), 0);
                float invertY = Game.configInvertY.IntValue > 0 ? 1.0f : -1.0f;
                eu += Time.deltaTime * (new Vector3(- invertY * Input.GetAxisRaw("RightStickY")*InputSystem.s_JoystickLookSensitivity.y, Input.GetAxisRaw("RightStickX") * InputSystem.s_JoystickLookSensitivity.x, 0));
                camera.transform.localEulerAngles = eu;
                m_DetachedMoveSpeed += Input.GetAxisRaw("Mouse ScrollWheel");
                float verticalMove = (Input.GetKey(KeyCode.R) ? 1.0f : 0.0f) + (Input.GetKey(KeyCode.F) ? -1.0f : 0.0f);
                verticalMove += Input.GetAxisRaw("Trigger");
                camera.transform.Translate(new Vector3(Input.GetAxisRaw("Horizontal"), verticalMove, Input.GetAxisRaw("Vertical")) * Time.deltaTime * m_DetachedMoveSpeed);
            }

            if (debugCameraMove.IntValue > 0)
            {
                // Only show for one player
                if (lastUsedFrame < Time.frameCount)
                {
                    lastUsedFrame = Time.frameCount;

                    int o = Time.frameCount % movehist_x.Length;
                    var rot = camera.transform.localEulerAngles;
                    movehist_x[o] = rot.x % 90.0f;
                    movehist_y[o] = rot.y % 90.0f;
                    movehist_z[o] = rot.z % 90.0f;

                    DebugOverlay.DrawGraph(4, 4, 10, 5, movehist_x, o, Color.red, 10.0f);
                    DebugOverlay.DrawGraph(4, 12, 10, 5, movehist_y, o, Color.green, 10.0f);
                    DebugOverlay.DrawGraph(4, 20, 10, 5, movehist_z, o, Color.blue, 10.0f);
                }
            }
        }
    }

    // Debugging graphs to show player movement in 3 axis
    static float[] movehist_x = new float[100];
    static float[] movehist_y = new float[100];
    static float[] movehist_z = new float[100];
    static float lastUsedFrame;

    [ConfigVar(Name = "debug.cameramove", Description = "Show graphs of first person camera rotation", DefaultValue = "0")]
    public static ConfigVar debugCameraMove;
    [ConfigVar(Name = "debug.cameradetach", Description = "Detach player camera from player", DefaultValue = "0")]
    public static ConfigVar debugCameraDetach;

    float m_DetachedMoveSpeed = 4.0f;
}