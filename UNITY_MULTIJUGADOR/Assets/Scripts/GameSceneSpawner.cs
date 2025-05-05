using System.Linq;
using UnityEngine;
using Unity.Netcode;
using UnityEngine.SceneManagement;

public class GameSceneSpawner : NetworkBehaviour
{
    [Header("Drag your PlayerAvatar prefab here")]
    [SerializeField]
    private GameObject playerAvatarPrefab;

    // Prevent spamming the RPC or redrawing the button
    private bool _spawnRequested = false;

    private void OnGUI()
    {
        // 1) Only pure clients (not host)
        if (!NetworkManager.Singleton.IsClient || NetworkManager.Singleton.IsHost)
            return;

        // 2) Only if they don't already have a player object
        if (NetworkManager.Singleton.SpawnManager.GetLocalPlayerObject() != null)
            return;

        // 3) Only in a “game” scene (assumes buildIndex 0 is your menu)
        if (SceneManager.GetActiveScene().buildIndex == 0)
            return;

        // 4) Only draw once
        if (_spawnRequested) return;

        // 5) Draw a simple centered button
        var rect = new Rect(
            (Screen.width - 160) / 2,
            (Screen.height - 30) / 2,
            160, 30
        );
        if (GUI.Button(rect, "Spawn"))
        {
            _spawnRequested = true;
            RequestSpawnServerRpc();
        }
    }

    /// <summary>
    /// Called on the client when they click “Spawn”.
    /// Server will instantiate at a unique Spawnpoint and
    /// call SpawnAsPlayerObject(owner) so the client gets ownership.
    /// </summary>
    [ServerRpc(RequireOwnership = false)]
    private void RequestSpawnServerRpc(ServerRpcParams rpc = default)
    {
        ulong owner = rpc.Receive.SenderClientId;

        // Find all spawn points in the scene
        var points = GameObject.FindGameObjectsWithTag("Spawnpoint");
        Vector3 pos = Vector3.zero;
        Quaternion rot = Quaternion.identity;

        if (points.Length > 0)
        {
            // Round-robin by clientId
            int idx = (int)(owner % (ulong)points.Length);
            pos = points[idx].transform.position;
            rot = points[idx].transform.rotation;
        }

        // Instantiate at that position
        var go = Instantiate(playerAvatarPrefab, pos, rot);

        // IMPORTANT: prefab must be registered in NetworkManager.NetworkPrefabs
        var netObj = go.GetComponent<NetworkObject>();
        netObj.SpawnAsPlayerObject(owner /* gives them ownership */, true);
        Debug.Log($"[Server] Spawned avatar {netObj.NetworkObjectId} for client {owner} at Spawnpoint {pos}");
    }
}