using UnityEngine;

[CreateAssetMenu()]
public class GameConfiguration: ScriptableObject
{
    [Header("Player movement")]
    public float playerSpeed = 6.0f;
    public float playerSprintSpeed = 6.0f;
    public float playerAcceleration = 30.0f;
    public float playerFriction = 6.0f;
    public float playerAiracceleration = 3.0f;
    public float playerAirFriction = 3.0f;
    public float playerGravity = 9.82f;
    public bool easterBunny = false;
    public float jumpAscentDuration = 0.2f;
    public float jumpAscentHeight = 1f;
    public float maxFallVelocity = 10;
}
