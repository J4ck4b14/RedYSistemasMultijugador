using UnityEngine;

public class ElevatorTrigger : MonoBehaviour
{
    public ElevatorCabin cabin;

    private void OnTriggerEnter(Collider other)
    {
        Debug.Log($"[CABIN TRIGGER] OnTriggerEnter by {other.name}");
        if (!other.CompareTag("Player")) return;

        if (other.TryGetComponent<PlayerAvatar>(out var player))
        {
            Debug.Log($"[CABIN TRIGGER] Registering {player.name}");
            cabin.OnPlayerEnterCabin();
            player.SetNearbyElevatorCabin(cabin);
        }
    }
    private void OnTriggerExit(Collider other)
    {
        Debug.Log($"[CABIN TRIGGER] OnTriggerExit by {other.name}");
        if (!other.CompareTag("Player")) return;

        if (other.TryGetComponent<PlayerAvatar>(out var player))
        {
            Debug.Log($"[CABIN TRIGGER] Clearing {player.name}");
            cabin.OnPlayerExitCabin();
            player.ClearNearbyElevatorCabin(cabin);
        }
    }
}
