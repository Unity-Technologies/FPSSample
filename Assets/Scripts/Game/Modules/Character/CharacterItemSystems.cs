using Unity.Collections;
using Unity.Entities;
using UnityEngine;


[DisableAutoCreation]
public class ApplyPresentationStateToItems : BaseComponentSystem    
{
    struct ItemGroup
    {
        public EntityArray entities;
        public ComponentArray<CharacterItem> items;
        public ComponentArray<AnimStateController> animStateCtrls;
    }
    
    struct Item1PGroup
    {
        public EntityArray entities;
        public ComponentArray<CharacterItem1P> item1Ps;
        public ComponentArray<AnimStateController> animStateCtrls;
    }

    [Inject]
    ItemGroup m_itemGroup;

    [Inject]
    Item1PGroup m_item1PGroup;
    
    public ApplyPresentationStateToItems(GameWorld world) : base(world) {}
    
    protected override void OnUpdate()
    {
        var deltaTime = m_world.frameDuration;
        for (var i = 0; i < m_itemGroup.animStateCtrls.Length; i++)
        {
            var entity = m_itemGroup.entities[i];
            var animStateCtrl = m_itemGroup.animStateCtrls[i];
            var item = m_itemGroup.items[i];
            var animState = EntityManager.GetComponentData<CharAnimState>(item.character);
            
            EntityManager.SetComponentData(entity, animState);
            animStateCtrl.ApplyPresentationState(m_world.worldTime, deltaTime);
        }
        
        for (var i = 0; i < m_item1PGroup.animStateCtrls.Length; i++)
        {
            var entity = m_item1PGroup.entities[i];
            var animStateCtrl = m_item1PGroup.animStateCtrls[i];
            var item1P = m_item1PGroup.item1Ps[i];
            
            if (!EntityManager.Exists(item1P.item))    
                continue;

            var item = EntityManager.GetComponentObject<CharacterItem>(item1P.item);

            var animState = EntityManager.GetComponentData<CharAnimState>(item.character);

            EntityManager.SetComponentData(entity, animState);

            animStateCtrl.ApplyPresentationState( m_world.worldTime, deltaTime);
        }
    }

}

[DisableAutoCreation]
public class CharacterItemLateUpdate : BaseComponentSystem<CharacterItem>
{
    public CharacterItemLateUpdate(GameWorld world) : base(world)
    {
        ExtraComponentRequirements = new[] {ComponentType.Subtractive<CharacterItem1P>()};
    }

    protected override void Update(Entity entity, CharacterItem item)
    {
        if (!EntityManager.Exists(item.character))
            return;
        
        var character = EntityManager.GetComponentObject<Character>(item.character);

        item.SetVisible(character.isVisible);

        if (!item.visible)
            return;
        
        item.transform.position = character.itemAttachBone.position;
        item.transform.rotation = character.itemAttachBone.rotation;
        
    }
}
