using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;
using Unity.Entities;
using UnityEngine.Profiling;

[DisableAutoCreation]
public class HandleServerProjectileRequests : BaseComponentSystem
{
	ComponentGroup Group;

	public HandleServerProjectileRequests(GameWorld world, BundledResourceManager resourceSystem) : base(world)
	{
		m_resourceSystem = resourceSystem;
    
		m_settings = Resources.Load<ProjectileModuleSettings>("ProjectileModuleSettings");
	}

	protected override void OnCreateManager()
	{
		base.OnCreateManager();
		Group = GetComponentGroup(typeof(ProjectileRequest));
	}

	protected override void OnDestroyManager()
	{
		base.OnDestroyManager();
		Resources.UnloadAsset(m_settings);
	}

	protected override void OnUpdate()
	{
		var entityArray = Group.GetEntityArray();
		var requestArray = Group.GetComponentDataArray<ProjectileRequest>();
		
		// Copy requests as spawning will invalidate Group 
		var requests = new ProjectileRequest[requestArray.Length];
		for (var i = 0; i < requestArray.Length; i++)
		{
			requests[i] = requestArray[i];
			PostUpdateCommands.DestroyEntity(entityArray[i]);
		}

		// Handle requests
		var projectileRegistry = m_resourceSystem.GetResourceRegistry<ProjectileRegistry>();
		foreach (var request in requests)
		{
			var registryIndex = projectileRegistry.FindIndex(request.projectileAssetGuid);
			if (registryIndex == -1)
			{
				GameDebug.LogError("Cant find asset guid in registry");
				continue;
			}

			var projectileEntity = m_settings.projectileFactory.Create(EntityManager,m_resourceSystem, m_world);

			var projectileData = EntityManager.GetComponentData<ProjectileData>(projectileEntity);
			projectileData.SetupFromRequest(request, registryIndex);
			projectileData.Initialize(projectileRegistry);
			
			PostUpdateCommands.SetComponent(projectileEntity, projectileData);
			PostUpdateCommands.AddComponent(projectileEntity, new UpdateProjectileFlag());
		}
	}

	BundledResourceManager m_resourceSystem;
	ProjectileModuleSettings m_settings;
}
