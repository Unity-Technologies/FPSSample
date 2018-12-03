using Unity.Entities;

[DisableAutoCreation]
public class TeleporterSystemServer : ComponentSystem
{

    public TeleporterSystemServer(GameWorld gameWorld)
    {
        m_GameWorld = gameWorld;
    }

    protected override void OnCreateManager()
    {
        base.OnCreateManager();
        m_TeleporterServerGroup = GetComponentGroup(typeof(TeleporterServer));
    }

    protected override void OnUpdate()
    {
        var teleporters = m_TeleporterServerGroup.GetComponentArray<TeleporterServer>();
        for (int i = 0, c = teleporters.Length; i < c; i++)
        {
            var t = teleporters[i];

            if (t.characterInside != null)
            {
                Character character = null;

                if (t.characterInside.owner != null)
                    character = t.characterInside.owner.GetComponent<Character>();

                if (character != null)
                {
                    var dstPos = t.targetTeleporter.GetSpawnPositionWorld();
                    var dstRot = t.targetTeleporter.GetSpawnRotationWorld();

                    character.TeleportTo(dstPos, dstRot);

                    t.targetTeleporter.GetComponent<TeleporterPresentation>().effectTick = m_GameWorld.worldTime.tick;
                }
                t.characterInside = null;

            }
        }
    }

    GameWorld m_GameWorld;
    private ComponentGroup m_TeleporterServerGroup;
}
