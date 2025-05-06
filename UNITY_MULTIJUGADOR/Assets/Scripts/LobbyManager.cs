using System.Collections.Generic;
using System.Linq;
using TMPro;
using Unity.Netcode;
using UnityEngine;

public enum GameMode : int
{
    FreeForAll = 0,
    Teams = 1,
    CaptureTheFlag = 2
}

/// <summary>
/// <para>Tracks host’s chosen game mode and each client's selection in the lobby.</para>
/// Updates a UI text field via ClientRpc to show server status and count of waiting players for the active mode.
/// </summary>
public class LobbyManager : NetworkBehaviour
{
    [Header("UI")]
    [Tooltip("TMP_Text to display server status, mode, and waiting count")]
    public TMP_Text statusText;

    /// <summary>Mode selected by the host (or server) for this lobby</summary>
    public NetworkVariable<int> currentGameMode = new NetworkVariable<int>(-1,
        NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    // Server-side mapping of clientId -> chosen mode
    private readonly Dictionary<ulong, int> clientSelections = new Dictionary<ulong, int>();

    private void OnClientConnected(ulong clientId)
    {
        clientSelections[clientId] = -1;
        UpdateClients();
    }

    private void OnClientDisconnected(ulong clientId)
    {
        clientSelections.Remove(clientId);
        UpdateClients();
    }

    /// <summary>
    /// Recomputes the waiting count and pushes an update to all clients.
    /// </summary>
    private void UpdateClients()
    {
        bool serverUp = NetworkManager.Singleton.IsHost || NetworkManager.Singleton.IsServer;
        int mode = currentGameMode.Value;
        int waitingCount = clientSelections.Values.Count(v => v == mode);

        UpdateStatusClientRpc(serverUp, mode, waitingCount);
    }

    /// <summary>
    /// Runs on every client to update the lobby status UI.
    /// </summary>
    [ClientRpc]
    private void UpdateStatusClientRpc(bool serverUp, int mode, int waitingCount)
    {
        statusText.text =
            $"Server: {(serverUp ? "Online" : "Offline")}\n" +
            $"Mode: {(mode >= 0 ? ((GameMode)mode).ToString() : "None")}\n" +
            $"Waiting Players: {waitingCount}";
    }

    new private void OnDestroy()
    {
        if (NetworkManager.Singleton != null && IsServer)
        {
            NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnected;
        }
    }

    /// <summary>How many clients (including host) have selected the current mode.</summary>
    public NetworkVariable<int> waitingCount = new NetworkVariable<int>(0);

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            // Whenever a client connects/disconnects, recalc
            NetworkManager.Singleton.OnClientConnectedCallback += _ => RecalculateWaitingCount();
            NetworkManager.Singleton.OnClientDisconnectCallback += _ => RecalculateWaitingCount();
        }
    }

    /// <summary>Call this from any place you change a client's mode or the host changes the lobby mode.</summary>
    private void RecalculateWaitingCount()
    {
        int mode = currentGameMode.Value;
        // Count server-spawned clients whose selectedGameMode == mode
        int count = NetworkManager.Singleton.ConnectedClientsList
            .Count(c => c.PlayerObject
                       .GetComponent<PlayerAvatar>()
                       .selectedGameMode.Value == mode);
        waitingCount.Value = count;
    }

    [ServerRpc(RequireOwnership = false)]
    public void SetClientModeServerRpc(int mode, ServerRpcParams rpcParams = default)
    {
        // record that client’s choice
        var clientAvatar = NetworkManager.Singleton.ConnectedClients[rpcParams.Receive.SenderClientId]
                               .PlayerObject.GetComponent<PlayerAvatar>();
        clientAvatar.selectedGameMode.Value = mode;

        // if host called it, also set the lobby‐wide mode
        if (IsHost) currentGameMode.Value = mode;

        // now update the waitingCount on the server
        RecalculateWaitingCount();
    }
}
