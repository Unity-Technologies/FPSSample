using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

public class ManageTerraformerController : StateMachineBehaviour
{
    public bool enableBehavior = true;
    public string sprintBoolName;
    public string sprintEnterVelocityFloatName;
    public string sprintExitVelocityFloatName;
    public string sprintWeightName;
    public string sprintToRunTriggerName;
    public string runToSprintTriggerName;

    float m_SprintWeight;

    int sprintBoolParam;
    int enterSpeedParam;
    int exitSpeedParam;
    int sprintWeightParam;
    int sprintToRunParam;
    int runToSprintParam;

    bool wasSprinting;

    void OnEnable()
    {
        sprintBoolParam = Animator.StringToHash(sprintBoolName);
        enterSpeedParam = Animator.StringToHash(sprintEnterVelocityFloatName);
        exitSpeedParam = Animator.StringToHash(sprintExitVelocityFloatName);
        sprintWeightParam = Animator.StringToHash(sprintWeightName);
        sprintToRunParam = Animator.StringToHash(sprintToRunTriggerName);
        runToSprintParam = Animator.StringToHash(runToSprintTriggerName);
    }
    
    public override void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        UpdateSprintWeight(animator);
    }


    void UpdateSprintWeight(Animator animator)
    {   
        if (!enableBehavior)
            return;

        var isSprinting = animator.GetBool(sprintBoolParam);
        
        if (isSprinting)
        {
            var enterSpeed = animator.GetFloat(enterSpeedParam);
            m_SprintWeight = Mathf.Clamp01(m_SprintWeight + enterSpeed * Time.deltaTime);

        }
        else
        {
            var exitSpeed = animator.GetFloat(exitSpeedParam);
            m_SprintWeight = Mathf.Clamp01(m_SprintWeight - exitSpeed * Time.deltaTime);
        }

        animator.SetFloat(sprintWeightParam, Mathf.SmoothStep(0f, 1f, m_SprintWeight));


        // TODO: (sunek) Consider looking at weight sprint weight as a condition too 
        if (!isSprinting & wasSprinting)
        {
            animator.SetTrigger(sprintToRunParam);
            animator.ResetTrigger(runToSprintParam);
        }

        if (isSprinting & !wasSprinting)
        {
            animator.SetTrigger(runToSprintParam);
        }
        
        wasSprinting = isSprinting;
    }
}
