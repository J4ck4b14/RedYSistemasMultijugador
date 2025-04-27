using UnityEngine;
using Unity.Netcode;
using System.Collections;
using Unity.Netcode.Components;

/// <summary>
/// <para>Elevator cabin that travels between two floors.</para>
/// Shows indicator lights on each floor to signal cabin presence,
/// closes doors, moves, and opens the destination door.
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
    [Tooltip("Door at the bottom floor")]
    public ElevatorDoor bottomDoor;

    [Tooltip("Door at the top floor")]
    public ElevatorDoor topDoor;

    [Header("Indicator Lights")]
    [Tooltip("Renderers whose emissive color shows presence at bottom floor")]
    public Renderer[] bottomFloorLights;

    [Tooltip("Renderers whose emissive color shows presence at top floor")]
    public Renderer[] topFloorLights;

    [Tooltip("Color when the light is on")]
    public Color onColor = new Color(0.75f, 0.4f, 0f, 1f);

    [Tooltip("Color when the light is off")]
    public Color offColor = Color.black;

    #endregion

    #region Internal State

    private bool isMoving = false;

    /// <summary>Door that just closed and triggered the move</summary>
    private ElevatorDoor activeDoor;

    /// <summary>Door that originally requested the move</summary>
    private ElevatorDoor targetDoor;

    /// <summary>Transform of the floor we're heading toward</summary>
    private Transform targetFloor;

    /// <summary>How many players are inside right now</summary>
    private int playersInsideTheCabin;

    #endregion

    #region Network Spawning & Initial Indicator

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (IsServer)
        {
            // Set initial lights based on starting position
            bool atBottom = IsAtThisFloor(bottomDoor);
            SetIndicatorLightsClientRpc(atBottom);
        }
    }

    #endregion

    #region Public API

    /// <summary>
    /// Is the cabin aligned (within 0.1 units) with the floor of this door?
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
    /// Called by ElevatorDoor.OpenDoor() so we know which door to watch for closing.
    /// </summary>
    public void SetActiveDoor(ElevatorDoor door)
    {
        activeDoor = door;
        Debug.Log($"[CABIN] Active door set to: {door.name}");
    }

    /// <summary>
    /// Called by a door to summon the cabin to its floor.
    /// Handles both remote calls and "inside‐cabin" calls.
    /// </summary>
    public void RequestCabinToThisFloor(ElevatorDoor door)
    {
        if (isMoving) return;

        bool sameFloor = IsAtThisFloor(door);
        // the door on the other floor (destination when inside)
        ElevatorDoor otherDoor = (door == bottomDoor) ? topDoor : bottomDoor;
        Transform otherFloor = (door == bottomDoor) ? topFloor : bottomFloor;

        if (sameFloor)
        {
            if (playersInsideTheCabin > 0)
            {
                // Player inside wants to go opposite
                targetDoor = otherDoor;
                targetFloor = otherFloor;
                activeDoor = door;
                door.ForceClose();
            }
            else
            {
                Debug.Log("[CABIN] Already here & empty --> ignoring.");
                return;
            }
        }
        else
        {
            // Remote call from another floor
            targetDoor = door;
            targetFloor = (door == bottomDoor) ? bottomFloor : topFloor;
            // close the door where the cabin currently sits
            ElevatorDoor currentDoor = IsAtThisFloor(bottomDoor) ? bottomDoor : topDoor;
            activeDoor = currentDoor;
            currentDoor.ForceClose();
        }

        Debug.Log($"[CABIN] Will close {activeDoor.name} then move.");
    }

    /// <summary>
    /// Called by ElevatorDoor when it finishes closing.
    /// If it's the activeDoor, begin the move coroutine.
    /// </summary>
    public void NotifyDoorClosed(ElevatorDoor door)
    {
        Debug.Log($"[CABIN] Notified that {door.name} closed (active is {activeDoor?.name}).");
        if (door != activeDoor) return;

        if (targetFloor != null && targetDoor != null)
        {
            StartCoroutine(MoveCabinRoutine());
        }
    }

    /// <summary>Called by ElevatorTrigger when a player enters the cabin volume.</summary>
    public void OnPlayerEnterCabin()
    {
        playersInsideTheCabin++;
    }

    /// <summary>Called by ElevatorTrigger when a player leaves the cabin volume.</summary>
    public void OnPlayerExitCabin()
    {
        playersInsideTheCabin = Mathf.Max(0, playersInsideTheCabin - 1);
    }

    #endregion

    #region Movement & Indicator Update

    /// <summary>
    /// Moves the cabin, opens the destination door, and updates indicator lights.
    /// </summary>
    private IEnumerator MoveCabinRoutine()
    {
        isMoving = true;

        // slide to target floor
        while (Vector3.Distance(transform.position, targetFloor.position) > 0.01f)
        {
            transform.position = Vector3.MoveTowards(
                transform.position,
                targetFloor.position,
                moveSpeed * Time.deltaTime
            );
            yield return null;
        }
        transform.position = targetFloor.position;

        // update lights on all clients
        bool nowAtBottom = (targetFloor == bottomFloor);
        SetIndicatorLightsClientRpc(nowAtBottom);

        // open the door at the arrival floor
        targetDoor.OpenDoor();

        // reset state
        activeDoor = targetDoor;
        targetDoor = null;
        targetFloor = null;
        isMoving = false;
    }

    #endregion

    #region Indicator Light RPC & Helpers

    /// <summary>
    /// Tell all clients to update their emissive indicator lights.
    /// </summary>
    [ClientRpc]
    private void SetIndicatorLightsClientRpc(bool isAtBottomFloor)
    {
        UpdateIndicatorLights(isAtBottomFloor);
    }

    /// <summary>
    /// Locally adjust each renderer's _EmissionColor via MaterialPropertyBlock.
    /// </summary>
    private void UpdateIndicatorLights(bool isAtBottomFloor)
    {
        // bottom-floor lights
        foreach (var rend in bottomFloorLights)
            SetEmissiveColor(rend, isAtBottomFloor ? onColor : offColor);

        // top-floor lights
        foreach (var rend in topFloorLights)
            SetEmissiveColor(rend, isAtBottomFloor ? offColor : onColor);
    }

    /// <summary>
    /// Utility to set a renderer's emissive color without creating new materials.
    /// </summary>
    private void SetEmissiveColor(Renderer rend, Color color)
    {
        var block = new MaterialPropertyBlock();
        rend.GetPropertyBlock(block);
        block.SetColor("_EmissionColor", color);
        rend.SetPropertyBlock(block);
    }

    #endregion

    #region "Go to Other Floor" RPC for Inside-Cabin

    /// <summary>
    /// Allows any client (RequireOwnership=false) to request a ride to the opposite floor.
    /// </summary>
    [ServerRpc(RequireOwnership = false)]
    public void RequestOtherFloorServerRpc(ServerRpcParams rpcParams = default)
    {
        // pick the other door and reuse our existing logic
        ElevatorDoor toDoor = IsAtThisFloor(bottomDoor) ? topDoor : bottomDoor;
        RequestCabinToThisFloor(toDoor);
    }

    #endregion
}
