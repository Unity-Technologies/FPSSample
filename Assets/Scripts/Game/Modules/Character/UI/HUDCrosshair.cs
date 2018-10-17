using UnityEngine;
using UnityEngine.UI;

public static class MathHelper // TODO where to put this`?
{
    public static float SignedAngle(float a, float b)
    {
        var difference = b - a;
        while (difference < -180) difference += 360;
        while (difference > 180) difference -= 360;
        return difference;
    }
}   


public class HUDCrosshair : MonoBehaviour
{
    [ConfigVar(Name = "debug.hudmarker", DefaultValue = "0", Description = "Debug show hud markers")]
    static ConfigVar debugHudMarker;

    public float hitIndicatorShowDuration = 0.5f;
    public float deathIndicatorShowDuration = 1.0f;
    public float damageDirectionShowDuration = 1.0f;
    public RawImage hitMaker;
    public RawImage deathMarker;
    public RawImage damageIndicator;

    void Awake()
    {
        hitMaker.gameObject.SetActive(false);
        deathMarker.gameObject.SetActive(false);
        damageIndicator.gameObject.SetActive(false);
    }

    public void FrameUpdate(PlayerCameraSettings cameraSettings)
    {
        if(debugHudMarker.IntValue > 0)
        {
            if (debugHudMarker.IntValue == 1)
            {
                hitMaker.gameObject.SetActive(true);
                m_hideHitIndicatorTime = Time.time + 5.0f;
            }
            else if (debugHudMarker.IntValue == 2)
            {
                deathMarker.gameObject.SetActive(true);
                m_hideDeathIndicatorTime = Time.time + 5.0f;
            }
            debugHudMarker.Value = "0";
        }

        if (hitMaker.gameObject.activeSelf)
        {
            if (Time.time > m_hideHitIndicatorTime)
                hitMaker.gameObject.SetActive(false);
        }
        if (deathMarker.gameObject.activeSelf)
        {
            if (Time.time > m_hideDeathIndicatorTime)
                deathMarker.gameObject.SetActive(false);
        }
        if (damageIndicator.gameObject.activeSelf)
        {
            UpdateHitDirectionIndicator(cameraSettings.rotation);
            if (Time.time > m_hideHitDirectionTime)
                damageIndicator.gameObject.SetActive(false);
        }
    }

    public void ShowHitMarker(bool lethal)
    {
        hitMaker.gameObject.SetActive(true);
        m_hideHitIndicatorTime = Time.time + hitIndicatorShowDuration; 

        if (lethal)
        {
            m_hideDeathIndicatorTime = Time.time + deathIndicatorShowDuration;
            deathMarker.gameObject.SetActive(true);
        }
    }

    public void UpdateHitDirectionIndicator(Quaternion cameraRotation)
    {
        var cameraForward = cameraRotation * Vector3.forward;
        var camDirPlane = Vector3.ProjectOnPlane(cameraForward, Vector3.up);
        var camAngle = Vector3.Angle(Vector3.forward, camDirPlane);
        var cross2 = Vector3.Cross(Vector3.forward, camDirPlane);
        if (cross2.y < 0)
            camAngle = -camAngle;

        var angle = MathHelper.SignedAngle(camAngle, m_damageAngle);

        var rot = Quaternion.Euler(0, 0, 180 - angle);
        damageIndicator.transform.rotation = rot;
    }
    
    public void ShowHitDirectionIndicator(float damageAngle)
    {
        m_damageAngle = damageAngle;
        damageIndicator.gameObject.SetActive(true);
        m_hideHitDirectionTime = Time.time + damageDirectionShowDuration;
    }

    float m_damageAngle;
    float m_hideHitIndicatorTime;
    float m_hideDeathIndicatorTime;
    float m_hideHitDirectionTime;
}
