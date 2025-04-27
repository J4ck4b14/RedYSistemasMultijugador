using UnityEngine;

public class ElevatorTrigger : MonoBehaviour
{
    public ElevatorCabin cabin;

    private void OnTriggerEnter(Collider other)
    {
        if(other.CompareTag("Player") && other.TryGetComponent<PlayerAvatar>(out var player))
        {
            cabin.OnPlayerEnterCabin();
            player.SetNearbyElevatorCabin(cabin);
            Debug.Log($"[CABIN TRIGGER] {player.name} entered cabin");
        }
    }
    private void OnTriggerExit(Collider other)
    {
        if(other.CompareTag("Player") && other.TryGetComponent<PlayerAvatar>( out var player))
        {
            cabin.OnPlayerExitCabin();
            player.ClearNearbyElevatorCabin(cabin);
            Debug.Log($"[CABIN TRIGGER] {player.name} exited cabin");
        }
    }
}
