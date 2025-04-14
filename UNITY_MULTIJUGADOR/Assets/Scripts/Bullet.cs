using UnityEngine;
using Unity.Netcode;

public class Bullet : NetworkBehaviour
{
    public NetworkVariable<Color> bulletColor = new NetworkVariable<Color>();
    public ParticleSystem hitEffectPrefab;
    private bool destroyed = false;

    /// <summary>
    /// Initializes bullet color when spawned
    /// </summary>
    public override void OnNetworkSpawn()
    {
        GetComponent<MeshRenderer>().material.color = bulletColor.Value;
    }

    /// <summary>
    /// Handles collision detection (server only)
    /// </summary>
    private void OnCollisionEnter(Collision collision)
    {
        if (!IsServer || destroyed) return;

        destroyed = true;

        if (collision.gameObject.CompareTag("Player"))
        {
            collision.gameObject.GetComponent<PlayerAvatar>().DamagePlayer();
        }

        ShowHitEffectClientRpc(collision.GetContact(0).point);
        GetComponent<NetworkObject>().Despawn(true);
    }

    /// <summary>
    /// Shows hit effect on all clients
    /// </summary>
    [ClientRpc]
    private void ShowHitEffectClientRpc(Vector3 pos)
    {
        if(hitEffectPrefab!=null)
        Instantiate(hitEffectPrefab, pos, Quaternion.identity);
        else return;
    }
}
