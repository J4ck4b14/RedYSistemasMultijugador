using UnityEngine;

public class ElevatorTrigger : MonoBehaviour
{
    public ElevatorCabin cabin;

    private void OnTriggerEnter(Collider other)
    {
        if(other.CompareTag("Player") && other.TryGetComponent<PlayerAvatar>(out var player))
        {
            cabin.OnPlayerEnterCabin();
        }
    }
    private void OnTriggerExit(Collider other)
    {
        if(other.CompareTag("Player") && other.TryGetComponent<PlayerAvatar>( out var player))
        {
            cabin.OnPlayerExitCabin();
        }
    }
}
