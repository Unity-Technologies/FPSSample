using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ActiveSubmenuTracker : MonoBehaviour
{
    void Start()
    {
        m_Animator = GetComponent<Animator>();
    }

    void Update()
    {
        if (Game.game && Game.game.clientFrontend)
        {
            m_Animator.SetInteger("Menu_Number", Game.game.clientFrontend.ActiveMainMenuNumber);
        }
        else
            m_Animator.SetInteger("Menu_Number", -1);
    }

    Animator m_Animator;
}
