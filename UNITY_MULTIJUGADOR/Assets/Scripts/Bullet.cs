using UnityEngine;
using Unity.Netcode;

/// <summary>
/// Represents a bullet in a network game environment
/// This class handles bullet behaviour (collision detection, impact effects, ...)
/// </summary>
public class Bullet : NetworkBehaviour
{
    // NetworkVariable for the bullet's color
    public NetworkVariable<Color> bulletColor = new NetworkVariable<Color>();
    // Particle system for the hits
    public ParticleSystem hitEffectPrefab;
    // Variable to handle the bullet's collisions
    private bool destroyed = false;

    public void OnNetworkSpawn()
    {
        GetComponent<MeshRenderer>().material.color = bulletColor.Value;
    }
    private void OnCollisionEnter(Collision collision)
    {
        if (destroyed)
            return;

        destroyed = true;

        // Cuando el servidor detecta una colision de bala con un jugador
        if (collision.gameObject.CompareTag("Player"))
        {
            collision.gameObject.GetComponent<PlayerAvatar>().DamagePlayer();
            destroyed = true;
        }

        // Show the effect on the clients
        ShowHitEffectClientRpc(collision.GetContact(0).point);
        GetComponent<NetworkObject>().Despawn(true);
    }

    /// <summary>
    /// Spawns a certain ParticleSystem prefab in the acquired position
    /// </summary>
    /// <param name="pos"></param>
    [ClientRpc]
    private void ShowHitEffectClientRpc(Vector3 pos)
    {
        Instantiate(hitEffectPrefab, pos, Quaternion.identity);
    }
}
