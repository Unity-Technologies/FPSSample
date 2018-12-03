using Unity.Entities;
using UnityEngine;

[DisableAutoCreation]
public class HandleSpatialEffectRequests : BaseComponentSystem 
{

	ComponentGroup Group;

	public HandleSpatialEffectRequests(GameWorld world, GameObject systemRoot, BundledResourceManager resourceSystem) : base(world)
	{
		var effectBundle = resourceSystem.GetResourceRegistry<SpatialEffectRegistry>();
		GameDebug.Assert(effectBundle != null,"No HitscanEffectRegistry defined in registry");

		m_Pools = new Pool[effectBundle.entries.Count];
		for(var i=0;i<effectBundle.entries.Count;i++)
		{
			var entry = effectBundle.entries[i];

			if (entry.prefab.guid == "")
				continue;
			
			var resource = resourceSystem.LoadSingleAssetResource(entry.prefab.guid);
			GameDebug.Assert(resource != null);

			var prefab = resource as GameObject;
			GameDebug.Assert(prefab != null);
			
			var pool = new Pool();
			pool.instances = new SpatialEffectInstance[entry.poolSize];
			for (var j = 0; j < pool.instances.Length; j++)
			{
				var go = GameObject.Instantiate(prefab);
            
				if(systemRoot != null)
					go.transform.SetParent(systemRoot.transform, false);

				pool.instances[j] = go.GetComponent<SpatialEffectInstance>();
				GameDebug.Assert(pool.instances[j],"Effect prefab does not have SpatialEffectInstance component");
			}

			m_Pools[i] = pool;
		}
	}

	protected override void OnCreateManager()
	{
		base.OnCreateManager();
		Group = GetComponentGroup(typeof(SpatialEffectRequest));
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
		var requestArray = Group.GetComponentDataArray<SpatialEffectRequest>();
		var entityArray = Group.GetEntityArray();
		
		for (var i = 0; i < requestArray.Length; i++)
		{
			var request = requestArray[i];
			var pool = m_Pools[request.effectTypeRegistryId];
			var index = pool.nextInstanceId % pool.instances.Length;
			pool.instances[index].StartEffect(request.position,request.rotation);
			pool.nextInstanceId++;
			
			PostUpdateCommands.DestroyEntity(entityArray[i]);
		}
	}

	class Pool
	{
		public SpatialEffectInstance[] instances;
		public int nextInstanceId;
	}

	Pool[] m_Pools;
}