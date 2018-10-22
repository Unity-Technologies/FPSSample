using UnityEngine;

[CreateAssetMenu(menuName = "FPS Sample/Item/TypeRegistry", fileName = "ItemTypeRegistry")]
public class ItemRegistry : Registry<ItemTypeDefinition>
{
    public int GetIndexByClientGUID(string guid)
    {
        for (var i = 0; i < entries.Length; i++)
        {
            if (entries[i].prefabClient.guid == guid)
                return i;
        }

        return -1;
    }
}