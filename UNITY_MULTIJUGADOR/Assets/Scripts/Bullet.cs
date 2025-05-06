using UnityEngine;
using Unity.Netcode;
using TMPro;

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

    [Header("Prefabs & Settings")]
    [Tooltip("Prefab for the wall impact decal.")]
    public GameObject decalPrefab;

    [Tooltip("Prefab for the floating damage text (should have a DamageText component).")]
    public GameObject damageTextPrefab;

    [Tooltip("Damage amount applied to players.")]
    public float damageAmount = 10f;

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

        var hitObj = collision.gameObject;
        string tag = hitObj.tag;

        // If we hit a wall, spawn a decal at the contact point
        if (tag == "Wall")
        {
            ContactPoint contact = collision.contacts[0];
            Vector3 position = contact.point;
            Quaternion rotation = Quaternion.LookRotation(contact.normal);

            Instantiate(decalPrefab, position, rotation);
        }

        // Destroy bullet on any collision
        Destroy(gameObject);
        // If we hit a player, damage them (duh...)
        if (collision.gameObject.CompareTag("Player"))
        {
            ulong shooterId = OwnerClientId;
            collision.gameObject.GetComponent<PlayerAvatar>().DamagePlayer(shooterId);
            // We could do something like make the damage points float up and fade out
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

/// <summary>
/// Handles bullet collision: spawns a decal on walls and floating damage text on players.
/// Attach this to your bullet prefab.
/// </summary>
public class BulletCollisionHandler : MonoBehaviour
{
    

    private void OnCollisionEnter(Collision collision)
    {
        
    }
}

