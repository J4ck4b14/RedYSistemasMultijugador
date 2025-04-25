using UnityEngine;
using Unity.Netcode;
using System.Collections;

/// <summary>
/// <para>Smart sliding door for an elevator floor.</para>
/// Handles opening/closing, reopens if interrupted, and syncs with the cabin logic.
/// </summary>
public class ElevatorDoor : NetworkBehaviour
{
    #region Settings

    [Header("Sliding Settings")]
    [Tooltip("How far the door slides when opening")]
    public float slideDistance = 2f;

    [Tooltip("Speed of the door movement")]
    public float slideSpeed = 2f;

    [Tooltip("How long the door waits before trying to close")]
    public float waitBeforeClosing = 2f;

    [Header("References")]
    [Tooltip("The mesh or visual part of the door that moves")]
    public Transform doorVisual;

    [Tooltip("The cabin this door belongs to")]
    public ElevatorCabin cabin;

    #endregion

    #region Internal State

    private Vector3 closedPosition;
    private Vector3 openPosition;

    private bool isOpen = false;
    private bool isMoving = false;

    private Coroutine doorRoutine;

    #endregion

    /* -------------------------------------------------------------------------- */
    /*                                 LIFECYCLE                                  */
    /* -------------------------------------------------------------------------- */

    private void Start()
    {
        // Set reference points for sliding movement
        closedPosition = doorVisual.localPosition;
        openPosition = closedPosition + Vector3.left * slideDistance;
    }

    /* -------------------------------------------------------------------------- */
    /*                             TRIGGER / INTERACTION                          */
    /* -------------------------------------------------------------------------- */

    /// <summary>
    /// If a player enters while the door is closing, reopen it
    /// </summary>
    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        if (other.TryGetComponent<PlayerAvatar>(out var player))
        {
            player.SetNearbyElevatorDoor(this);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        if (other.TryGetComponent<PlayerAvatar>(out var player))
        {
            player.ClearNearbyElevatorDoor(this);
        }
    }
    /// <summary>
    /// Called by the player when pressing "E" near this door
    /// </summary>
    [ServerRpc(RequireOwnership = false)]
    public void TryOpenFromPlayerServerRpc(ServerRpcParams rpcParams = default)
    {
        Debug.Log($"[SERVER] Elevator door '{name}' received interaction.");

        if (cabin.IsAtThisFloor(this))
        {
            Debug.Log("[SERVER] Cabin is at floor — opening door.");
            OpenDoor();
        }
        else
        {
            Debug.Log("[SERVER] Cabin not here — summoning it.");
            cabin.RequestCabinToThisFloor(this);
        }
    }


    /* -------------------------------------------------------------------------- */
    /*                                DOOR CONTROL                                */
    /* -------------------------------------------------------------------------- */
    #region Door Control
    /// <summary>
    /// Starts the full open-wait-close routine
    /// </summary>
    public void OpenDoor()
    {
        if (doorRoutine != null)
            StopCoroutine(doorRoutine);

        cabin.SetActiveDoor(this);

        doorRoutine = StartCoroutine(OpenAndCloseRoutine());
    }

    public void ForceClose()
    {
        if(doorRoutine != null) StopCoroutine(doorRoutine);

        isOpen = false;
        isMoving = true;

        doorRoutine = StartCoroutine(CloseOnlyRoutine());
    }

    /// <summary>
    /// Coroutine that opens the door, waits, then closes
    /// </summary>
    private IEnumerator OpenAndCloseRoutine()
    {
        isOpen = true;
        isMoving = true;

        yield return SlideDoor(openPosition); // open

        isMoving = false;
        yield return new WaitForSeconds(waitBeforeClosing);

        isMoving = true;
        isOpen = false;

        yield return SlideDoor(closedPosition); // close

        isMoving = false;

        // Inform cabin that door is fully closed
        cabin.NotifyDoorClosed(this);
    }

    /// <summary>
    /// Handles the smooth slide motion of the door
    /// </summary>
    private IEnumerator SlideDoor(Vector3 target)
    {
        while (Vector3.Distance(doorVisual.localPosition, target) > 0.01f)
        {
            doorVisual.localPosition = Vector3.MoveTowards(
                doorVisual.localPosition,
                target,
                slideSpeed * Time.deltaTime
            );
            yield return null;
        }

        doorVisual.localPosition = target; // Snap to end position
    }

    private IEnumerator CloseOnlyRoutine()
    {
        yield return SlideDoor(closedPosition);
        isMoving = false;

        cabin.NotifyDoorClosed(this);
    }
#endregion
}
