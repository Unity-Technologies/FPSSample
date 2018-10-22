using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ServerCameraSystem
{
    public void Shutdown()
    {
        Cleanup();
    }

    public ServerCameraSystem(GameWorld gameWorld)
    {
        m_CameraSpots = Object.FindObjectsOfType<ServerCameraSpot>();

        // NOTE : We should look at this when we have figured out the final 
        // way that cameras should work
        GameObject cameraGO = GameObject.Instantiate<GameObject>(Resources.Load<GameObject>("Prefabs/ServerCam"));
        m_Camera = cameraGO.GetComponent<Camera>();

        if (m_Camera)
            Game.game.PushCamera(m_Camera);

        Console.AddCommand("debug.servercam_shots", CmdServercamShots, "Grab a screenshot from each of the servercams");
    }

    public void OnBeforeLevelUnload()
    {
        Cleanup();
    }

    public void Update()
    {
        if (m_Camera == null)
            return;

        var t = Time.realtimeSinceStartup;

        if(m_NextCapture > -1)
        {
            m_NextCaptureTick--;
            if(m_NextCaptureTick == 5)
                NextCamera();
            if(m_NextCaptureTick <= 0)
            {
                Console.EnqueueCommand("screenshot");
                m_NextCapture--;
                m_NextCaptureTick = 10;
            }
            return;
        }

        m_Camera.transform.localEulerAngles = m_OriginalOrientation + new Vector3(
                Mathf.Sin(t * k_IdleCycle.x) * k_IdleLevel.x,
                Mathf.Sin(t * k_IdleCycle.y) * k_IdleLevel.y,
                Mathf.Sin(t * k_IdleCycle.z) * k_IdleLevel.z
                );

        if (t > m_NextSwitchTime)
            NextCamera();

        if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.LeftArrow))
            NextCamera();
    }

    void Cleanup()
    {
        m_CameraSpots = null;
        m_Camera = null;
        m_NextCameraSpot = 0;
    }

    void NextCamera()
    {
        if (m_Camera == null || m_CameraSpots == null || m_CameraSpots.Length == 0)
            return;

        var spot = m_CameraSpots[m_NextCameraSpot];

        m_Camera.transform.position = spot.transform.position;
        m_Camera.transform.rotation = spot.transform.rotation;

        m_OriginalOrientation = m_Camera.transform.localEulerAngles;
        m_NextSwitchTime = Time.realtimeSinceStartup + 5.0f;
        m_NextCameraSpot = (m_NextCameraSpot + 1) % m_CameraSpots.Length;
    }

    void CmdServercamShots(string[] args)
    {
        if (m_NextCapture > -1)
        {
            Console.Write("Already capturing!");
            return;
        }
        if(m_CameraSpots == null || m_CameraSpots.Length == 0)
        {
            Console.Write("No server cam spots!");
            return;
        }
        Console.SetOpen(false);
        m_NextCapture = m_CameraSpots.Length - 1;
        m_NextCaptureTick = 10;
        m_NextCameraSpot = 0;
    }

    int m_NextCapture = -1;
    int m_NextCaptureTick = 0;

    // Genuine QuakeWorld Idle Cam Constants!!!
    private static readonly Vector3 k_IdleCycle = new Vector3(1.0f, 2.0f, 0.5f);
    private static readonly Vector3 k_IdleLevel = new Vector3(0.1f, 0.3f, 0.3f);

    private Camera m_Camera;

    private Vector3 m_OriginalOrientation;
    private ServerCameraSpot[] m_CameraSpots;
    private int m_NextCameraSpot;
    private float m_NextSwitchTime;
}
