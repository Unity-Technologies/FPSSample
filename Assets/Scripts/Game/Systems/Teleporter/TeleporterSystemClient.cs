using Unity.Entities;

[DisableAutoCreation]
public class TeleporterSystemClient : ComponentSystem
{
	public struct Teleporters
	{
		public ComponentArray<TeleporterPresentation> teleporterPresentations;
		public ComponentArray<TeleporterClient> teleporterClients;
        
	}

	[Inject]
	Teleporters teleporters;

	public TeleporterSystemClient(GameWorld gameWorld)
	{
		m_GameWorld = gameWorld;
	}

	protected override void OnUpdate()
	{
		for(int i = 0, c = teleporters.teleporterClients.Length; i < c; i++)
		{
			var teleporterPresentation = teleporters.teleporterPresentations[i];
			var teleporterClient = teleporters.teleporterClients[i];
			
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
