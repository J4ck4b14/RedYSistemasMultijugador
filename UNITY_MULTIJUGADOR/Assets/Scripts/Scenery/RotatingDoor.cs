using UnityEngine;
using Unity.Netcode;
using Unity;

/// <summary>
/// <para>Simple networked door that rotates open/closed when players press E.</para>
/// Uses a pivot for smooth rotation and syncs the state across clients.
/// </summary>
public class RotatingDoor : NetworkBehaviour
{
    #region Settings

    [Header("Door Settings")]
    [Tooltip("How much the door rotates when opened (in degrees)")]
    public float rotationAngle = 90f;

    [Tooltip("How fast the door rotates (higher = faster)")]
    public float rotationSpeed = 2f;

    #endregion

    #region Network Variables

    /// <summary>
    /// <para>Is the door currently open? Synced for everyone.</para>
    /// </summary>
    private NetworkVariable<bool> isOpen = new NetworkVariable<bool>(false);

    #endregion

    #region Internal State

    private Quaternion initialRotation;
    private Quaternion targetRotation;

    #endregion

    #region Unity Lifecycle

    private void Start()
    {
        // Save the starting rotation of the door
        initialRotation = transform.localRotation;

        // Calculate the target rotation when the door is opened
        targetRotation = Quaternion.Euler(0f, rotationAngle, 0f) * initialRotation;
    }

    private void Update()
    {

        // Smoothly rotate to the target state
        Quaternion goalRotation = isOpen.Value ? targetRotation : initialRotation;
        transform.localRotation = Quaternion.Lerp(transform.localRotation, goalRotation, Time.deltaTime * rotationSpeed);
    }

    // Called when player enters the door's trigger zone
    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        if (other.TryGetComponent<PlayerAvatar>(out var player))
        {
            player.SetNearbyRotatingDoor(this);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        if (other.TryGetComponent<PlayerAvatar>(out var player))
        {
            player.ClearNearbyRotatingDoor(this);
        }
    }

    #endregion

    #region Network Interaction

    /// <summary>
    /// <para>Request to toggle door state (runs on server)</para>
    /// Called by the local player when pressing E.
    /// </summary>
    [ServerRpc(RequireOwnership = false)]
    public void TryInteractServerRpc(ServerRpcParams rpcParams = default)
    {
        isOpen.Value = !isOpen.Value;
    }
    

    #endregion
}