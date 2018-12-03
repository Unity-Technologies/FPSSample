using Unity.Entities;

[DisableAutoCreation]
public class TeleporterSystemClient : ComponentSystem
{
	ComponentGroup Group;

	public TeleporterSystemClient(GameWorld gameWorld)
	{
		m_GameWorld = gameWorld;
	}

	protected override void OnCreateManager()
	{
		base.OnCreateManager();
		Group = GetComponentGroup(typeof(TeleporterPresentation), typeof(TeleporterClient));
	}

	protected override void OnUpdate()
	{
		var teleporterClientArray = Group.GetComponentArray<TeleporterClient>();
		var teleporterPresentationArray = Group.GetComponentArray<TeleporterPresentation>();
		
		for(int i = 0, c = teleporterClientArray.Length; i < c; i++)
		{
			var teleporterClient = teleporterClientArray[i];
			var teleporterPresentation = teleporterPresentationArray[i];
			
			if (teleporterClient.effectEvent.Update(m_GameWorld.worldTime, teleporterPresentation.effectTick))
			{
				if(teleporterClient.effect != null)
					SpatialEffectRequest.Create(PostUpdateCommands, teleporterClient.effect, 
						teleporterClient.effectTransform.position, teleporterClient.effectTransform.rotation);
			}
		}
	}

	GameWorld m_GameWorld;
}
