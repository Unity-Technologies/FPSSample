using System.Xml;
using Unity.Entities;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Profiling;

public class HitscanEffect : MonoBehaviour
{
	public LineRenderer lineRenderer;
	
	public void StartEffect(Vector3 startPos, Vector3 endPos)
	{
		m_startPosition = startPos;
		m_endPosition =  endPos;
		m_time = 0;
		
		lineRenderer.enabled = true;
		UpdateLineRenderer();
	}

	public void CancelEffect()
	{
	}

	// TODO Dont use MonoBehavior.Update
	void Update() 
	{

		m_time += Time.deltaTime;

		if(lineRenderer.enabled)
			UpdateLineRenderer();
	}

	void UpdateLineRenderer()
	{
		Profiler.BeginSample("HitscanEffect.UpdateLineRenderer");

		var vel = 100;
		var length = 1;

		var totalMoveDist = m_time * vel;
		var deltaPos = m_endPosition - m_startPosition;
		
		if (totalMoveDist > deltaPos.magnitude)
		{
			lineRenderer.enabled = false;
			return;
		}
		
		var moveDir = deltaPos.normalized;
		var pos = m_startPosition + totalMoveDist * moveDir;
		lineRenderer.SetPosition(0,pos);
		lineRenderer.SetPosition(1,pos + moveDir*length);
		
		Profiler.EndSample();
	}
	

	private Vector3 m_startPosition;
	private Vector3 m_endPosition;
	private float m_time;
}
