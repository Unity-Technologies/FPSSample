using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.PostProcessing;

[RequireComponent(typeof(Camera))]
public class GameCamera : MonoBehaviour
{
    Camera m_Camera;

    private void Awake()
    {
        m_Camera = GetComponent<Camera>();
    }

    private bool pushed = false;
    private void Update()
    {
        if (Game.game == null)
            return;

        if (!pushed)
        {
            pushed = true;
            Game.game.PushCamera(m_Camera);
        }
    }

    private void OnDisable()
    {
        if (Game.game)
            Game.game.PopCamera(m_Camera);
        else
            GameDebug.LogWarning("Unable to pop GameCamera " + gameObject.name + ". No Game.game");
    }
}
