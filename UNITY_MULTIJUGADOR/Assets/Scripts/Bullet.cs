using UnityEngine;
using Unity.Netcode;
using TMPro;

/// <summary>
/// <para>Bullet projectile that flies through the air and wrecks players.</para>
/// Handles collision detection and visual effects when bullets hit things.
/// </summary>
public class Bullet : NetworkBehaviour
{
    public enum HitEffectType : byte
    {
        Wall,
        Blood
    }

    [Tooltip("Bullet color - syncs across network")]
    public NetworkVariable<Color> bulletColor = new NetworkVariable<Color>();

    [Tooltip("Particle effect to play on hit")]
    public ParticleSystem bloodEffectPrefab;

    [Tooltip("Particle effect to play on hit")]
    public ParticleSystem wallEffectPrefab;

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

        ContactPoint contact = collision.contacts[0];

        // If we hit a wall, spawn a decal at the contact point
        if (tag == "Wall")
        {
            NetworkObject netObj = hitObj.GetComponent<NetworkObject>();

            if (netObj != null)
            {
                ShowWallDecalClientRpc(contact.point, transform.forward, contact.normal, netObj);
            }
            else
            {
                // Pass an empty reference — decal will still spawn without parenting
                ShowWallDecalClientRpc(contact.point, transform.forward, contact.normal, default);
            }

            ShowHitEffectClientRpc(contact.point, contact.normal, HitEffectType.Wall);
        }

        // If we hit a player, damage them (duh...)
        if (collision.gameObject.CompareTag("Player"))
        {
            ulong shooterId = OwnerClientId;
            var avatar = collision.gameObject.GetComponent<PlayerAvatar>();
            int realDamage = avatar.DamagePlayer(OwnerClientId); // Like Phil Swift with Flex Seal says...

            Vector3 hitPoint = contact.point;
            Vector3 directionToShooter = (transform.forward).normalized;
            Vector3 offset = directionToShooter * 0.01f;

            // Instantiate floating damage text on all clients
            ShowDamageTextClientRpc(hitPoint, realDamage);
            ShowHitEffectClientRpc(hitPoint + offset, directionToShooter, HitEffectType.Blood); // Blood slightly off the character for clearer vision
        }

        GetComponent<NetworkObject>().Despawn(true);

        // Destroy bullet on any collision
        Destroy(gameObject);
    }

    /// <summary>
    /// Make fancy particles (GLITTER[or bl00d]) appear when bullet hits something (ALL THE GIRLS ARE GIRLING, GIRLING)
    /// </summary>
    [ClientRpc]
    private void ShowHitEffectClientRpc(Vector3 pos, Vector3 directionToShooter, HitEffectType effectType)
    {
        ParticleSystem prefabToSpawn = null;

        switch (effectType)
        {
            case HitEffectType.Wall:
                prefabToSpawn = wallEffectPrefab;
                break;
            case HitEffectType.Blood:
                prefabToSpawn = bloodEffectPrefab;
                break;
        }

        if (prefabToSpawn == null) return;

        // Face the shooter (blood sprays away from the body)
        Quaternion rotation = Quaternion.LookRotation(directionToShooter);
        Instantiate(prefabToSpawn, pos, rotation);
    }
    
    [ClientRpc]
    private void ShowDamageTextClientRpc(Vector3 pos, float damage)
    {
        if (damageTextPrefab == null) return;

        GameObject obj = Instantiate(damageTextPrefab, pos, Quaternion.identity);
        var dmgText = obj.GetComponent<DamageText>();
        if (dmgText != null)
        {
            dmgText.Initialize(damage);
        }
    }

    [ClientRpc]
    private void ShowWallDecalClientRpc(Vector3 position, Vector3 forward, Vector3 normal, NetworkObjectReference targetRef)
    {
        if (decalPrefab == null) return;

        Vector3 offsetPos = position + normal * 0.025f;
        Quaternion rotation = Quaternion.LookRotation(forward);

        GameObject decal = Instantiate(decalPrefab, offsetPos, rotation);

        // Try to parent the decal if the target was passed
        if (targetRef.TryGet(out NetworkObject targetObj))
        {
            decal.transform.SetParent(targetObj.transform, true);
        }
    }
}

