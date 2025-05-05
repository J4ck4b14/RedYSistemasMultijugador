using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using Unity.Netcode;
using UnityEngine.SceneManagement;

/// <summary>
/// Simple lobby manager:
/// 1) Clients pick a mode via UI → PickMode()
/// 2) Server locks in on first pick: sets currentMode, requiredCount, and scenes
/// 3) Tracks how many have chosen that mode
/// 4) When count ≥ requiredCount, server calls Netcode to load all game scenes in sync
/// Also exposes GetTeam() for Teams/CTF modes.
/// </summary>
public class LobbyManager : NetworkBehaviour
{
    public static LobbyManager Instance { get; private set; }

    [Header("Mode Settings")]
    [Tooltip("How many players each mode requires")]
    public List<GameModePlayerRequirement> gameModeRequirements;
    [Tooltip("Which build indices to load per mode")]
    public List<GameModeSceneConfig> gameModeSceneConfigs;

    // Networked counters for UI feedback
    public NetworkVariable<int> playerPickCount = new NetworkVariable<int>(0);
    public NetworkVariable<int> requiredCount = new NetworkVariable<int>(0);

    // Internal state
    private readonly Dictionary<ulong, GameMode> clientPicks = new();
    private bool hasGameStarted = false;

    [HideInInspector] public GameMode currentMode;
    [HideInInspector] public int[] selectedBuildIndices = new int[0];

    private void Awake()
    {
        // Singleton pattern
        if (Instance != null && Instance != this) Destroy(gameObject);
        else Instance = this;
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        // Update HUD whenever counts change
        playerPickCount.OnValueChanged += (_, _) => RefreshUI();
        requiredCount.OnValueChanged += (_, _) => RefreshUI();
        RefreshUI();
    }

    /// <summary>
    /// Called by client UI when you click a mode button.
    /// </summary>
    public void PickMode(GameMode mode)
        => PickModeServerRpc((int)mode);

    [ServerRpc(RequireOwnership = false)]
    private void PickModeServerRpc(int modeInt, ServerRpcParams rpc = default)
    {
        var clientId = rpc.Receive.SenderClientId;
        var pick = (GameMode)modeInt;
        clientPicks[clientId] = pick;

        // On first pick: lock in everything
        if (requiredCount.Value == 0)
        {
            currentMode = pick;

            // Set how many are needed
            requiredCount.Value = gameModeRequirements
                .First(r => r.gameMode == pick)
                .requiredPlayers;

            // Cache which scenes to load
            selectedBuildIndices = gameModeSceneConfigs
                .First(c => c.gameMode == pick)
                .sceneBuildIndices;
        }

        TryStartGame();
    }

    /// <summary>
    /// Server counts how many chose the locked mode; if ≥ required, begin.
    /// </summary>
    private void TryStartGame()
    {
        if (!IsServer || hasGameStarted) return;

        int count = clientPicks.Values.Count(m => m == currentMode);
        playerPickCount.Value = count;

        if (count >= requiredCount.Value)
        {
            hasGameStarted = true;
            BeginSceneLoad();
        }
    }

    /// <summary>
    /// Updates your lobby HUD via LobbyStatusDisplay.
    /// </summary>
    private void RefreshUI()
    {
        var disp = FindFirstObjectByType<LobbyStatusDisplay>();
        if (disp == null) return;

        int curr = playerPickCount.Value;
        int req = requiredCount.Value;
        bool ready = (req > 0 && curr >= req);

        disp.UpdateStatus(
            req == 0
               ? "Select a mode…"
               : $"Players: {curr}/{req}",
            req == 0
               ? ""
               : ready
                   ? "Loading Game…"
                   : $"Waiting {curr}/{req}"
        );
    }

    /// <summary>
    /// Server‐only: tells Netcode to load all selectedBuildIndices in sync.
    /// </summary>
    private void BeginSceneLoad()
    {
        if (!IsServer) return;

        for (int i = 0; i < selectedBuildIndices.Length; i++)
        {
            string path = SceneUtility.GetScenePathByBuildIndex(selectedBuildIndices[i]);
            string name = Path.GetFileNameWithoutExtension(path);
            var mode = (i == 0) ? LoadSceneMode.Single : LoadSceneMode.Additive;

            NetworkManager.SceneManager.LoadScene(name, mode);
        }
    }

    /// <summary>
    /// For Team/CTF modes: splits choosing clients in 0/1 round‐robin; FFA = –1.
    /// </summary>
    public int GetTeam(ulong clientId)
    {
        if (currentMode == GameMode.FreeForAll) return -1;

        var list = clientPicks
            .Where(kv => kv.Value == currentMode)
            .Select(kv => kv.Key)
            .ToList();

        int idx = list.IndexOf(clientId);
        return idx < 0 ? -1 : (idx % 2);
    }
}
