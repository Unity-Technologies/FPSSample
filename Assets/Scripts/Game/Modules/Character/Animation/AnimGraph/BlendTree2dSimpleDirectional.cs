using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Animations;
using UnityEngine.Assertions;

[Serializable]
public struct BlendSpaceNode
{
    public AnimationClip clip;
    public Vector2 position;
    public float speed;
    [HideInInspector]
    public float weight;
    [HideInInspector]
    public float clipLength;
}

public class BlendTree2dSimpleDirectional
{
    public BlendTree2dSimpleDirectional(PlayableGraph graph, List<BlendSpaceNode> nodes)
    {
        m_Nodes = nodes;
        var count = m_Nodes.Count;
        m_Positions = new Vector2[count];
        m_Clips = new AnimationClipPlayable[count];
        m_Weights = new float[count];
        
        m_Mixer = AnimationMixerPlayable.Create(graph, count);
        m_Mixer.SetPropagateSetTime(true); 

        for (var i = 0; i < count; i++)
        {
            var node = m_Nodes[i];
            var clip = AnimationClipPlayable.Create(graph, node.clip);
            node.clipLength = node.clip.length;
            clip.Play();
            m_Mixer.ConnectInput(i, clip, 0);
            m_Clips[i] = clip;
            m_Nodes[i] = node;
        }

        masterSpeed = 1f;
        SetBlendPosition(new Vector2(0f, 0f));
    }

    public float SetBlendPosition(Vector2 position, bool updateGraph = true)
    {                    
        var count = m_Nodes.Count;
        for (var i = 0; i < count; i++)
        {
            m_Positions[i] = m_Nodes[i].position;
        }
        CalculateWeights(m_Positions, ref m_Weights, position);

        // Store results and calculate blendedClipLength
        m_BlendedClipLength = 0f;
        for (var i = 0; i < count; i++)
        {
            var node = m_Nodes[i];
            node.weight = m_Weights[i];
            m_Nodes[i] = node;
            m_BlendedClipLength += node.clipLength / m_Nodes[i].speed * node.weight;
        }

        GameDebug.Assert(m_BlendedClipLength > 0f, "blendedClipLength must be more that 0");
        m_BlendedClipLength /= masterSpeed;

        if (updateGraph)
        {
            UpdateGraph();
        }
        
        return m_BlendedClipLength;
    }

    public void UpdateGraph()
    {
        for (var i = 0; i < m_Clips.Length; i++)
        {  
            m_Mixer.SetInputWeight(i, m_Weights[i]);
            m_Clips[i].SetSpeed(m_Nodes[i].clipLength / m_BlendedClipLength);
            m_Clips[i].SetApplyFootIK(footIk);
        }
    }

    public void SetPhase(float phase)
    {
        for (var i = 0; i < m_Clips.Length; i++)
        {
            m_Clips[i].SetTime(phase * m_Clips[i].GetAnimationClip().length);
        }
    }
    
    static void CalculateWeights(Vector2[] positionArray, ref float[] weightArray, Vector2 blendParam)
    {
        var count = positionArray.Length;

        // Initialize all weights to 0
        for (var i = 0; i < weightArray.Length; i++)
        {
            weightArray[i] = 0f;
        }
        
        // Handle fallback
        if (count < 2)
        {
            if (count == 1)
                weightArray[0] = 1;
            return;
        }

        var blendPosition = new Vector2(blendParam.x, blendParam.y);

        // Handle special case when sampled ecactly in the middle
        if (blendPosition == new Vector2(0f, 0f))
        {
            // If we have a center motion, give that one all the weight
            for (var i = 0; i < count; i++)
            {
                if (positionArray[i] == Vector2.zero)
                {
                    weightArray[i] = 1;
                    return;
                }
            }

            // Otherwise divide weight evenly
            float sharedWeight = 1.0f / count;
            for (var i = 0; i < count; i++)
            {
                weightArray[i] = sharedWeight;
            }

            return;
        }

        int indexA = -1;
        int indexB = -1;
        int indexCenter = -1;
        float maxDotForNegCross = -100000.0f;
        float maxDotForPosCross = -100000.0f;
        for (var i = 0; i < count; i++)
        {
            if (positionArray[i] == Vector2.zero)
            {
                if (indexCenter >= 0)
                    return;
                indexCenter = i;
                continue;
            }

//            Vector2 posNormalized = Mathf.Normalize(positionArray[i]);
            Vector2 posNormalized = positionArray[i].normalized;
            var dot = Vector2.Dot(posNormalized, blendPosition);
            var cross = posNormalized.x * blendPosition.y - posNormalized.y * blendPosition.x;
            if (cross > 0f)
            {
                if (dot > maxDotForPosCross)
                {
                    maxDotForPosCross = dot;
                    indexA = i;
                }
            }
            else
            {
                if (dot > maxDotForNegCross)
                {
                    maxDotForNegCross = dot;
                    indexB = i;
                }
            }
        }

        float centerWeight = 0;

        if (indexA < 0 || indexB < 0)
        {
            // Fallback if sampling point is not inside a triangle
            centerWeight = 1;
        }
        else
        {
            var a = positionArray[indexA];
            var b = positionArray[indexB];

            // Calculate weights using barycentric coordinates
            // (formulas from http://en.wikipedia.org/wiki/Barycentric_coordinate_system_%28mathematics%29 )
            float det = b.y * a.x - b.x * a.y; // Simplified from: (b.y-0)*(a.x-0) + (0-b.x)*(a.y-0);

            // TODO: Is x and y used correctly below??
            float wA = (b.y * blendParam.x - b.x * blendParam.y) / det; // Simplified from: ((b.y-0)*(l.x-0) + (0-b.x)*(l.y-0)) / det;
            float wB = (a.x * blendParam.y - a.y * blendParam.x) / det; // Simplified from: ((0-a.y)*(l.x-0) + (a.x-0)*(l.y-0)) / det;
            centerWeight = 1 - wA - wB;

            // Clamp to be inside triangle
            if (centerWeight < 0)
            {
                centerWeight = 0;
                float sum = wA + wB;
                wA /= sum;
                wB /= sum;
            }
            else if (centerWeight > 1)
            {
                centerWeight = 1;
                wA = 0;
                wB = 0;
            }

            // Give weight to the two vertices on the periphery that are closest
            weightArray[indexA] = wA;
            weightArray[indexB] = wB;
        }

        if (indexCenter >= 0)
        {
            weightArray[indexCenter] = centerWeight;
        }
        else
        {
            // Give weight to all children when input is in the center
            float sharedWeight = 1.0f / count;
            for (var i = 0; i < count; i++)
                weightArray[i] += sharedWeight * centerWeight;
        }
    }

    public Playable GetRootPlayable()
    {
        return m_Mixer;
    }
    
    AnimationClipPlayable[] m_Clips;
    AnimationMixerPlayable m_Mixer;
    List<BlendSpaceNode> m_Nodes;
    Vector2[] m_Positions;
    float[] m_Weights;
    float m_BlendedClipLength;
    
    public float masterSpeed;
    public bool footIk;
}
