using UnityEngine;

public class IngameHUD : MonoBehaviour
{
    [ConfigVar(Name = "show.hud", DefaultValue = "1", Description = "Show HUD")]
    public static ConfigVar showHud;

    public HUDCrosshair m_Crosshair;
    public HUDGoal m_Goal;

    Canvas m_Canvas;

    public void Awake()
    {
        m_Canvas = GetComponent<Canvas>();
    }

    public void SetPanelActive(bool active)
    {
        gameObject.SetActive(active);
    }

    public void FrameUpdate(LocalPlayer localPlayer, PlayerCameraSettings cameraSettings)
    {
        var show = showHud.IntValue > 0;
        if (m_Canvas.enabled != show)
            m_Canvas.enabled = show;

        m_Crosshair.FrameUpdate(cameraSettings);
        m_Goal.FrameUpdate(localPlayer);
    }

    public void ShowHitMarker(bool lethal)
    {
        Game.SoundSystem.Play(lethal ? m_LethalHitSound : m_HitMarkerSound);
        m_Crosshair.ShowHitMarker(lethal);
    }

    [SerializeField] SoundDef m_HitMarkerSound;
    [SerializeField] SoundDef m_LethalHitSound;
}
