using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Utility class for interpolating values. Usage
//
//    var ival = new Interpolator(0.0f, Interpolator.CurveType.Linear)
//    ival.MoveTo(10.0f, 0.5f); // interpolate from 0 to 10 in 0.5 secs
//
//    ival.GetValue();          // gets current value
//
//    ival.SetValue(12.0f);     // force value and stop animation
//
// Ideas for more curve types: http://sol.gfxile.net/interpolation/ 

public class Interpolator
{
    public enum CurveType
    {
        Linear,
        SmoothDeparture,
        SmoothArrival,
        SmoothStep
    }

    public float targetValue { get { return m_TargetValue; } }

    public Interpolator(float startValue, CurveType type)
    {
        m_Type = type;
        SetValue(startValue);
    }

    public void SetValue(float value)
    {
        m_StartValue = value;
        m_TargetValue = value;
        m_StartTime = 0;
        m_TargetTime = 0;
    }

    public void MoveTo(float target, float time)
    {
        m_StartValue = GetValue();
        m_TargetValue = target;
        m_StartTime = Time.realtimeSinceStartup;
        m_TargetTime = m_StartTime + time;
    }

    public bool IsMoving()
    {
        return Time.realtimeSinceStartup < m_TargetTime;
    }

    public float Direction()
    {
        return Mathf.Sign(m_TargetTime - m_StartValue);
    }

    public void Stop()
    {
        m_StartValue = m_TargetValue = GetValue();
        m_TargetTime = 0;
        m_StartTime = 0;
    }

    public float GetValue()
    {
        float now = Time.realtimeSinceStartup;
        float timeToLive = m_TargetTime - now;
        if (timeToLive <= 0.0f)
            return m_TargetValue;

        float t = (now - m_StartTime) / (m_TargetTime - m_StartTime);
        switch (m_Type)
        {
            default:
            case CurveType.Linear:
                return m_StartValue + (m_TargetValue - m_StartValue) * t;
            case CurveType.SmoothArrival:
                var s = 1.0f - t;
                return m_StartValue + (m_TargetValue - m_StartValue) * (1.0f - s * s * s * s);
            case CurveType.SmoothDeparture:
                return m_StartValue + (m_TargetValue - m_StartValue) * t * t * t * t;
            case CurveType.SmoothStep:
                return m_StartValue + (m_TargetValue - m_StartValue) * t * t * (3.0f - 2.0f * t);
        }
    }

    CurveType m_Type;
    float m_StartTime;
    float m_StartValue;
    float m_TargetTime;
    float m_TargetValue;

}
