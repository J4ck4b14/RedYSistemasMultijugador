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

    // Variable to handle the bullet's collisions
    private bool destroyed = false;

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
    }
}
