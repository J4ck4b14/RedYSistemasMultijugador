using UnityEngine;
using TMPro;
using Unity.Netcode;

public class LobbyStatusDisplay : MonoBehaviour
{
    [Header("UI")]
    [Tooltip("TextMeshPro UGUI to show server & client count")]
    public TMP_Text statusText;

    void Start()
    {
        // Initial update
        UpdateStatus();

        // Subscribe to connection events
        NetworkManager.Singleton.OnClientConnectedCallback += OnClientChanged;
        NetworkManager.Singleton.OnClientDisconnectCallback += OnClientChanged;
    }

    void OnDestroy()
    {
        // Clean up
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback -= OnClientChanged;
            NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientChanged;
        }
    }

    private void OnClientChanged(ulong clientId)
    {
        UpdateStatus();
    }

    private void UpdateStatus()
    {
        bool serverUp = NetworkManager.Singleton.IsServer || NetworkManager.Singleton.IsHost;
        int clientCount = NetworkManager.Singleton.ConnectedClientsList.Count;

        statusText.text =
            $"Server: {(serverUp ? "Online" : "Offline")}\n" +
            $"Players: {clientCount}";
    }
}