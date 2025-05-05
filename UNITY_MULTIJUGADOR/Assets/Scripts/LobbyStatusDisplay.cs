using UnityEngine;
using TMPro;
using Unity.Netcode;

public class LobbyStatusDisplay : MonoBehaviour
{
    [Header("UI")]
    [Tooltip("TextMeshPro UGUI to show server & client count")]
    public TMP_Text statusText;

    [Tooltip("TextMeshPro UGUI to show the waiting animation/dots")]
    public TMP_Text waitingText;

    private void Start()
    {
        // Initial draw
        DrawStatus();

        // Subscribe to connection events
        NetworkManager.Singleton.OnClientConnectedCallback += _ => DrawStatus();
        NetworkManager.Singleton.OnClientDisconnectCallback += _ => DrawStatus();
    }

    private void OnDestroy()
    {
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback -= _ => DrawStatus();
            NetworkManager.Singleton.OnClientDisconnectCallback -= _ => DrawStatus();
        }
    }

    /// <summary>
    /// Called whenever the client/server count or mode changes.
    /// </summary>
    private void DrawStatus()
    {
        bool serverUp = NetworkManager.Singleton.IsServer || NetworkManager.Singleton.IsHost;
        int clientCount = NetworkManager.Singleton.ConnectedClientsList.Count;

        // Build your two lines
        string statusLine = $"Server: {(serverUp ? "Online" : "Offline")}\nPlayers: {clientCount}";
        string waitingLine = serverUp
            ? (clientCount >= 1 ? "Ready to pick a mode" : "Waiting for players")
            : "Starting as client...";

        UpdateStatus(statusLine, waitingLine);
    }

    /// <summary>
    /// Public API for LobbyManager to override status text.
    /// </summary>
    public void UpdateStatus(string statusMessage, string waitingMessage)
    {
        if (statusText != null) statusText.text = statusMessage;
        if (waitingText != null) waitingText.text = waitingMessage;
    }
}
