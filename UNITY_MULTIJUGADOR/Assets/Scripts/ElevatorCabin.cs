using UnityEngine;
using Unity.Netcode;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode.Components;

/// <summary>
/// <para>Elevator cabin that travels between two floors.</para>
/// Only moves when doors are closed, and opens the correct door when it arrives.
/// </summary>
[RequireComponent(typeof(NetworkObject))]
[RequireComponent(typeof(NetworkTransform))]
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

    private int playersInsideTheCabin;

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

    public void SetActiveDoor(ElevatorDoor door)
    {
        activeDoor = door;
        Debug.Log($"[CABIN] Active door is {(activeDoor)}");
    }

    /// <summary>
    /// Called by a door to summon the cabin to its floor
    /// </summary>
    public void RequestCabinToThisFloor(ElevatorDoor door)
    {
        if (isMoving) return;

        bool sameFloor = IsAtThisFloor(door);
        ElevatorDoor currentDoor = IsAtThisFloor(bottomDoor) ? bottomDoor : topDoor;
        ElevatorDoor otherDoor = (door == bottomDoor) ? topDoor : bottomDoor;
        Transform otherFloor = (door == bottomDoor) ? topFloor : bottomFloor;

        if (sameFloor)
        {
            if (playersInsideTheCabin > 0)
            {
                // Move to the opposite floor
                targetDoor = otherDoor;
                targetFloor = otherFloor;
                activeDoor = door;
                door.ForceClose();
            }
            else
            {
                Debug.Log("[CABIN] Already here - no one inside. Ignoring.");
                return;
            }
        }
        else
        {
            // Summon cabin to this floor
            targetDoor = door;
            targetFloor = (door == bottomDoor) ? bottomFloor: topFloor;
            activeDoor = currentDoor;
            currentDoor.ForceClose();
        }

        Debug.Log($"[CABIN] Closing {activeDoor.name} before moving");
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

    public void OnPlayerEnterCabin()
    {
        playersInsideTheCabin++;
    }

    public void OnPlayerExitCabin()
    {
        playersInsideTheCabin = Mathf.Max(0, playersInsideTheCabin - 1);
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
