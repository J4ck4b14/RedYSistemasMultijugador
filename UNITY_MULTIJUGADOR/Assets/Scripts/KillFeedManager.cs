using UnityEngine;
using Unity.Netcode;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Drawing;

public class KillFeedManager : NetworkBehaviour
{
    [Header("UI References")]
    [SerializeField] private Text killFeedText;
    [SerializeField] private float messageDuration = 5f;

    private Queue<string> killMessages = new Queue<string>();
    private float currentMessageTimer = 0f;

    /// <summary>
    /// Called on server when a kill occurs, it is responsible for the queueing of messages.
    /// </summary>
    /// <param name="killerId"></param>
    /// <param name="victimId"></param>
    public void RegisterKill(ulong killerId, ulong victimId)
    {
        if (!IsServer) return;

        // Get them bad boys responsible for all this mess
        NetworkObject killer = NetworkManager.Singleton.ConnectedClients[killerId].PlayerObject;
        NetworkObject victim = NetworkManager.Singleton.ConnectedClients[victimId].PlayerObject;

        // Get the color from the trouble makers >:C
        UnityEngine.Color killerColor = killer.GetComponent<PlayerAvatar>().playerColor.Value;
        UnityEngine.Color victimColor = victim.GetComponent<PlayerAvatar>().playerColor.Value;

        // Get their colors in hex strings, so we can later format the text
        string killerHex = ColorUtility.ToHtmlStringRGB(killerColor);
        string victimHex = ColorUtility.ToHtmlStringRGB(victimColor);

        // Format the text (u know, give it a lil spice, ma G)
        string message = $"<color=#{killerHex}>Player {killerId}</color> killed " + $"<color =#{victimHex}>Player {victimId}</color>";

        // Broadcast to all clients
        AddKillMessageClientRPC(message);
    }

    /// <summary>
    /// Executed in the clients, from the server, is, in short, how the messages appear to the user.
    /// </summary>
    /// <param name="message"></param>
    [ClientRpc]
    private void AddKillMessageClientRPC(string message)
    {
        if (killFeedText == null) return;

        // Add message to the queue
        killMessages.Enqueue(message);

        // Limit queue size
        if (killMessages.Count > 5)
        {
            killMessages.Dequeue();
        }

        UpdateKillFeedDisplay();
        currentMessageTimer = messageDuration;
    }

    private void FixedUpdate()
    {
        if (killMessages.Count == 0) return;

        currentMessageTimer -= Time.deltaTime;
        if (currentMessageTimer <= 0)
        {
            killMessages.Dequeue();
            UpdateKillFeedDisplay();
            currentMessageTimer = messageDuration;
        }
    }

    private void UpdateKillFeedDisplay()
    {
        //Combine messages with text breaks
        killFeedText.text = string.Join("\n", killMessages);
    }
}
