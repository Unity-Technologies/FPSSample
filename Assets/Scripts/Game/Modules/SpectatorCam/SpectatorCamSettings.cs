using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "SpectatorCamSettings", menuName = "SampleGame/SpectatorCam/SpectatorCamSettings")]
public class SpectatorCamSettings : ScriptableObject
{
	public WeakAssetReference spectatorCamPrefab;
}
