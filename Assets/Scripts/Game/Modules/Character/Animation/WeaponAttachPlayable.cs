using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Animations;

public class WeaponAttachPlayable : PlayableBehaviour
{

    public void Initialize(Transform weaponAttacher, Transform weaponRoot)
    {
        m_weaponAttacher = weaponAttacher;
        m_weaponRoot = weaponRoot;

        
    }

    public override void PrepareFrame(Playable playable, FrameData info)
    {
        if (m_weaponAttacher != null && m_weaponRoot != null)
        {
            m_weaponRoot.position = m_weaponAttacher.position;
            m_weaponRoot.rotation = m_weaponAttacher.rotation;
        }

        base.PrepareFrame(playable, info);
    }

    public override void ProcessFrame(Playable playable, FrameData info, object playerData)
    {
        //m_weaponRoot.position = m_weaponAttacher.position;
        //m_weaponRoot.rotation = m_weaponAttacher.rotation;

        //playable
        //AnimationPlayableGraphExtensions
        //AnimationPlayableUtilities..
        //PlayableOutputExtensions
        //PlayableExtensions
        //playable.GetInput()


        base.ProcessFrame(playable, info, playerData);
    }

    Transform m_weaponAttacher;
    Transform m_weaponRoot;
}

