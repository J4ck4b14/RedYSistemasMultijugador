using UnityEngine;
using Unity.Netcode;


public enum PowerUpType
{
    HP,
    PowerBullet,
    SpeedCola
}

public class PowerUp : NetworkBehaviour
{
    public PowerUpType type;

    private void OnTriggerEnter(Collider other)
    {
        if (!IsServer || !other.CompareTag("Player")) return;

        NetworkObject netObj = other.GetComponent<NetworkObject>();
        if (netObj != null)
        {
            // Enviamos un mensaje al cliente que recogió el Power-Up
            // para que sea ese cliente quien llame al ServerRpc
            NotifyPlayerClientRpc(netObj.OwnerClientId, type);

            // Despawning del Power-Up en red
            GetComponent<NetworkObject>().Despawn(true);
        }
    }
    // Este ClientRpc solo lo recibe el cliente que recogió el Power-Up
    [ClientRpc]
    private void NotifyPlayerClientRpc(ulong playerId, PowerUpType type)
    {
        Debug.Log($"[CLIENT] Recibido PowerUp {type} para PlayerId: {playerId}");
        var obj = NetworkManager.Singleton.SpawnManager.GetLocalPlayerObject();
        Debug.Log($"[CLIENT] Local client {NetworkManager.Singleton.LocalClientId} usando objeto {obj?.name}");

        // Asegurarse de que este mensaje solo lo procese el jugador correcto
        if (NetworkManager.Singleton.LocalClientId != playerId) return;
        // El cliente dueño llama a su propio ServerRpc
        PlayerAvatar localPlayer = NetworkManager.Singleton.SpawnManager.GetLocalPlayerObject().GetComponent<PlayerAvatar>();
        localPlayer.SendPowerUpToServerRpc(type);
    }
}