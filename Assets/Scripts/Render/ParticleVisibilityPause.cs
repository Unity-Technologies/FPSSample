using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public class ParticleVisibilityPause : MonoBehaviour
{
    private ParticleSystemRenderer psr;
    private ParticleSystem ps;
    private bool isVisible; //current state
    private void Start()
    {
        ps = GetComponent<ParticleSystem>();
        psr = GetComponent<ParticleSystemRenderer>();
        //set initially whether the system should be simulating
        isVisible = psr.isVisible;
    }
    private void Update()
    {
        if (psr.isVisible != isVisible)
        {
            isVisible = psr.isVisible;
            if (isVisible)
                ps.Play();
            else
                ps.Pause();
        }
    }
}