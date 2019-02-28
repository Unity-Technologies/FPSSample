using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "CharacterModuleSettings", menuName = "FPS Sample/Character/CharacterModuleSettings")]
public class CharacterModuleSettings : ScriptableObject
{
    public ReplicatedEntity character;
}
