using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

[AlwaysUpdateSystem]
[DisableAutoCreation]
public class HandleHitscanEffectRequests : BaseComponentSystem 
{
	struct HitscanEffectReques 
	{
		public HitscanEffectTypeDefinition effectDef;
		public Vector3 startPos;
		public Vector3 endPos;
	}
	
	List<HitscanEffectReques> m_requests = new List<HitscanEffectReques>(32);
	
	public void Request(HitscanEffectTypeDefinition effectDef, Vector3 startPos, Vector3 endPos)
	{
		m_requests.Add(new HitscanEffectReques
		{
			effectDef = effectDef,
			startPos = startPos,
			endPos = endPos,
		});
	}
	
	public HandleHitscanEffectRequests(GameWorld world) : base(world)
	{}

	protected override void OnUpdate()
	{
		for (int nRequest = 0; nRequest < m_requests.Count; nRequest++)
		{
			var request = m_requests[nRequest];
			
			if(request.effectDef.effect != null)
			{
				var vfxSystem = World.GetExistingManager<VFXSystem>();
				
				vfxSystem.SpawnLineEffect(request.effectDef.effect, request.startPos, request.endPos);
			}
		}
		m_requests.Clear();
	}
}
