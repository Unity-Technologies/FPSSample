using System;
using Unity.Entities;
using UnityEngine;

[DisallowMultipleComponent]
public class CharacterPresentationSetup : MonoBehaviour
{
    public GameObject geomtry;
    public Transform itemAttachBone;
    public AbilityUI[] uiPrefabs;    // TODO (mogensh) perhaps move UI to their own char presentation (so they are just just char and items)
    
    public Transform weaponBoneDebug;// TODO (mogensh) put these two debug properties somewhere appropriate
    public Vector3 weaponOffsetDebug;

    [NonSerialized] public Entity character;

    [NonSerialized] public bool updateTransform = true;
    [NonSerialized] public Entity attachToPresentation;
    
    public bool IsVisible
    {
        get { return isVisible; }
    }
    
    public void SetVisible(bool visible)
    {
        isVisible = visible;
        if(geomtry != null && geomtry.activeSelf != visible)  
            geomtry.SetActive(visible);
    }

    [NonSerialized] bool isVisible = true;
}

[DisableAutoCreation]
public class UpdatePresentationRootTransform : BaseComponentSystem<CharacterPresentationSetup>
{
    private ComponentGroup Group;
    
    public UpdatePresentationRootTransform(GameWorld world) : base(world) {}

    protected override void Update(Entity entity, CharacterPresentationSetup charPresentation)
    {
        if (!charPresentation.updateTransform)
            return;

        if (charPresentation.attachToPresentation != Entity.Null)
            return;

        var animState = EntityManager.GetComponentData<CharacterInterpolatedData>(charPresentation.character);
        charPresentation.transform.position = animState.position;
        charPresentation.transform.rotation = Quaternion.Euler(0f, animState.rotation, 0f);
    }
}

[DisableAutoCreation]
public class UpdatePresentationAttachmentTransform : BaseComponentSystem<CharacterPresentationSetup>
{
    public UpdatePresentationAttachmentTransform(GameWorld world) : base(world) {}

    protected override void Update(Entity entity, CharacterPresentationSetup charPresentation)
    {
        if (!charPresentation.updateTransform)
            return;
            
        if (charPresentation.attachToPresentation == Entity.Null)
            return;


        if (!EntityManager.Exists(charPresentation.attachToPresentation))
        {
            GameDebug.LogWarning("Huhb ?");
            return;
        }
        
        var refPresentation =
            EntityManager.GetComponentObject<CharacterPresentationSetup>(charPresentation.attachToPresentation);

        charPresentation.transform.position = refPresentation.itemAttachBone.position;
        charPresentation.transform.rotation = refPresentation.itemAttachBone.rotation;

    }
}