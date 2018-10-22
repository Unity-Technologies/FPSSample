using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "SpectatorCamSettings", menuName = "FPS Sample/SpectatorCam/SpectatorCamSettings")]
public class SpectatorCamSettings : ScriptableObject
{
	public WeakAssetReference spectatorCamPrefab;
}
