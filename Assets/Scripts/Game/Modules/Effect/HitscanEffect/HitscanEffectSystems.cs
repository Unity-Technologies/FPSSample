using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

[DisableAutoCreation]
public class HandleHitscanEffectRequests : BaseComponentSystem 
{
	struct Requests
	{
		public EntityArray entities;
		public ComponentDataArray<HitscanEffectRequest> requests;
	}

	[Inject] Requests RequestGroup;
	
	public HandleHitscanEffectRequests(GameWorld world, GameObject systemRoot, BundledResourceManager resourceSystem) : base(world)
	{
		var effectBundle = resourceSystem.GetResourceRegistry<HitscanEffectRegistry>();
		GameDebug.Assert(effectBundle != null,"No HitscanEffectRegistry defined in registry");

		m_Pools = new Pool[effectBundle.entries.Length];
		for(var i=0;i<effectBundle.entries.Length;i++)
		{
			var entry = effectBundle.entries[i]; 
			var resource = resourceSystem.LoadSingleAssetResource(entry.prefab.guid);
			GameDebug.Assert(resource != null);

			var prefab = resource as GameObject;
			GameDebug.Assert(prefab != null);
			
			var pool = new Pool();
			pool.instances = new HitscanEffect[entry.poolSize];
			for (var j = 0; j < pool.instances.Length; j++)
			{
				var go = GameObject.Instantiate(prefab);
            
				if(systemRoot != null)
					go.transform.SetParent(systemRoot.transform, false);

				pool.instances[j] = go.GetComponent<HitscanEffect>();
				GameDebug.Assert(pool.instances[j],"Effect prefab does not have HitscanEffect component");
			}

			m_Pools[i] = pool;
		}
	}

	protected override void OnDestroyManager()
	{
		if (m_Pools != null)
		{
			for (var i = 0; i < m_Pools.Length; i++)
			{
				var pool = m_Pools[i];
				for(var j=0;j<pool.instances.Length;j++)
					GameObject.Destroy(pool.instances[j]);
			}
		}		
	}

	protected override void OnUpdate()
	{
		for (var i = 0; i < RequestGroup.requests.Length; i++)
		{
			var request = RequestGroup.requests[i];

			var pool = m_Pools[request.effectTypeRegistryId - 1];
			
			var index = pool.nextInstanceId % pool.instances.Length;
			pool.instances[index].StartEffect(request.startPos, request.endPos);
			pool.nextInstanceId++;
			
			PostUpdateCommands.DestroyEntity(RequestGroup.entities[i]);
		}
	}

	class Pool
	{
		public HitscanEffect[] instances;
		public int nextInstanceId;
	}

	Pool[] m_Pools;
}
