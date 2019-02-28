using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

[AlwaysUpdateSystem]
[DisableAutoCreation]
public class HandleSpatialEffectRequests : BaseComponentSystem 
{
	struct SpatialEffectRequest 
	{
		public SpatialEffectTypeDefinition effectDef;
		public float3 position;
		public quaternion rotation;
	}

	
	List<SpatialEffectRequest> m_requests = new List<SpatialEffectRequest>(32);
	
	public HandleSpatialEffectRequests(GameWorld world) : base(world)
	{}


	public void Request(SpatialEffectTypeDefinition effectDef, float3 position, quaternion rotation)
	{
		m_requests.Add(new SpatialEffectRequest
		{
			effectDef = effectDef,
			position = position,
			rotation = rotation,
		});
	}
	
	protected override void OnUpdate()
	{
		for (int nRequest=0;nRequest<m_requests.Count;nRequest++)
		{
			var request = m_requests[nRequest];
			
			if(request.effectDef.effect != null)
			{
				var normal = math.mul(request.rotation,new float3(0,0,1));

				var vfxSystem = World.GetExistingManager<VFXSystem>();
				vfxSystem.SpawnPointEffect(request.effectDef.effect, request.position, normal);
			}

			if (request.effectDef.sound != null)
				Game.SoundSystem.Play(request.effectDef.sound, request.position);

			if (request.effectDef.shockwave.enabled)
			{
				var layer = LayerMask.NameToLayer("Debris");
				var mask = 1 << layer;
				var explosionCenter = request.position + (float3)UnityEngine.Random.insideUnitSphere * 0.2f;
				var colliders = Physics.OverlapSphere(request.position,request.effectDef.shockwave.radius,mask);
				for (var i = 0; i < colliders.Length; i++)
				{
					var rigidBody = colliders[i].gameObject.GetComponent<Rigidbody>();
					if (rigidBody != null)
					{
						rigidBody.AddExplosionForce(request.effectDef.shockwave.force,explosionCenter,
							request.effectDef.shockwave.radius, request.effectDef.shockwave.upwardsModifier, 
							request.effectDef.shockwave.mode);
					}
				}
			}


			/*
			var hdpipe = RenderPipelineManager.currentPipeline as UnityEngine.Experimental.Rendering.HDPipeline.HDRenderPipeline;
			if (hdpipe != null)
			{
			    var matholder = GetComponent<DecalHolder>();
			    if (matholder != null)
			    {
			        var ds = UnityEngine.Experimental.Rendering.HDPipeline.DecalSystem.instance;
			        var go = new GameObject();
			        go.transform.rotation = effectEvent.rotation;
			        go.transform.position = effectEvent.position;
			        go.transform.Translate(-0.5f, 0, 0, Space.Self);
			        go.transform.up = go.transform.right;
			        var d = go.AddComponent<UnityEngine.Experimental.Rendering.HDPipeline.DecalProjectorComponent>();
			        d.m_Material = matholder.mat;
			        ds.AddDecal(d);
			    }
			}
			*/
		}
	
		m_requests.Clear();
	}
}
