using UnityEngine;
using Unity.Netcode;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// <para>Elevator cabin that travels between two floors.</para>
/// Only moves when doors are closed, and opens the correct door when it arrives.
/// </summary>
public class ElevatorCabin : NetworkBehaviour
{
    #region Configuration

    [Header("Elevator Setup")]
    [Tooltip("Position of the bottom floor")]
    public Transform bottomFloor;

    [Tooltip("Position of the top floor")]
    public Transform topFloor;

    [Tooltip("Speed at which the elevator moves")]
    public float moveSpeed = 3f;

    [Header("Connected Doors")]
    public ElevatorDoor bottomDoor;
    public ElevatorDoor topDoor;

    #endregion

    #region Internal State

    private bool isMoving = false;

    /// <summary>
    /// Door that just closed and triggered the move
    /// </summary>
    private ElevatorDoor activeDoor;

    /// <summary>
    /// Door that called the elevator (from another floor)
    /// </summary>
    private ElevatorDoor targetDoor;

    /// <summary>
    /// Floor position to move toward
    /// </summary>
    private Transform targetFloor;

    #endregion

    /* -------------------------------------------------------------------------- */
    /*                                  INTERFACE                                 */
    /* -------------------------------------------------------------------------- */

    #region Public API

    /// <summary>
    /// Returns true if the elevator is currently aligned with this door's floor
    /// </summary>
    public bool IsAtThisFloor(ElevatorDoor door)
    {
        float threshold = 0.1f;
        if (door == bottomDoor)
            return Vector3.Distance(transform.position, bottomFloor.position) < threshold;
        else
            return Vector3.Distance(transform.position, topFloor.position) < threshold;
    }

    /// <summary>
    /// Called by a door to summon the cabin to its floor
    /// </summary>
    public void RequestCabinToThisFloor(ElevatorDoor door)
    {
        if (isMoving) return;
        if (IsAtThisFloor(door)) return;

        targetDoor = door;
        targetFloor = (door == bottomDoor) ? bottomFloor : topFloor;

        // NEW: Mark current floor as the active door
        activeDoor = (door == bottomDoor) ? topDoor : bottomDoor;
    }

    /// <summary>
    /// Called by a door when it finishes closing.
    /// Triggers elevator movement if needed.
    /// </summary>
    public void NotifyDoorClosed(ElevatorDoor door)
    {
        Debug.Log($"[CABIN] Notified that door '{door.name}' closed.");
        Debug.Log($"[CABIN] Active door is: '{(activeDoor ? activeDoor.name : "null")}'");

        if (door != activeDoor)
        {
            Debug.Log("[CABIN] Ignored — not the active door.");
            return;
        }

        if (targetFloor != null && targetDoor != null)
        {
            Debug.Log("[CABIN] Starting cabin movement.");
            StartCoroutine(MoveCabinRoutine());
        }
    }

    #endregion

    /* -------------------------------------------------------------------------- */
    /*                                 MOVEMENT                                   */
    /* -------------------------------------------------------------------------- */

    #region Movement System

    /// <summary>
    /// Smoothly moves the elevator toward the target floor
    /// </summary>
    private IEnumerator MoveCabinRoutine()
    {
        isMoving = true;

        while (Vector3.Distance(transform.position, targetFloor.position) > 0.01f)
        {
            transform.position = Vector3.MoveTowards(
                transform.position,
                targetFloor.position,
                moveSpeed * Time.deltaTime
            );
            yield return null;
        }

        // Snap into position
        transform.position = targetFloor.position;

        // Open the door at the arrival floor
        targetDoor.OpenDoor();

        // Update internal state
        activeDoor = targetDoor;
        targetDoor = null;
        targetFloor = null;
        isMoving = false;
    }

    #endregion
}
