using UnityEngine;
using Unity.Netcode;

/// <summary>
/// <para>Bullet projectile that flies through the air and wrecks players.</para>
/// Handles collision detection and visual effects when bullets hit things.
/// </summary>
public class Bullet : NetworkBehaviour
{
    [Tooltip("Bullet color - syncs across network")]
    public NetworkVariable<Color> bulletColor = new NetworkVariable<Color>();

    [Tooltip("Particle effect to play on hit")]
    public ParticleSystem hitEffectPrefab;

    private bool destroyed = false;

    /// <summary>
    /// Initializes bullet color when spawned
    /// </summary>
    public override void OnNetworkSpawn()
    {
        // Server sets initial color
        if (IsServer)
        {
            GetComponent<MeshRenderer>().material.color = bulletColor.Value;
        }

        // Clients listen for color changes
        bulletColor.OnValueChanged += (Color prev, Color current) => {
            GetComponent<MeshRenderer>().material.color = current;
        };
    }

    /// <summary>
    /// When we hit something, do damage and spawn effects
    /// </summary>
    private void OnCollisionEnter(Collision collision)
    {
        if (!IsServer || destroyed) return;

        destroyed = true;

        // If we hit a player, damage them (duh...)
        if (collision.gameObject.CompareTag("Player"))
        {
            ulong shooterId = OwnerClientId;
            collision.gameObject.GetComponent<PlayerAvatar>().DamagePlayer(shooterId);
        }

        // Show the hit effect on all clients
        ShowHitEffectClientRpc(collision.GetContact(0).point);
        GetComponent<NetworkObject>().Despawn(true);
    }

    /// <summary>
    /// Make fancy particles (GLITTER) appear when bullet hits something (ALL THE GIRLS ARE GIRLING, GIRLING)
    /// </summary>
    [ClientRpc]
    private void ShowHitEffectClientRpc(Vector3 pos)
    {
        if(hitEffectPrefab!=null)
        Instantiate(hitEffectPrefab, pos, Quaternion.identity);
        else return;
    }
}
