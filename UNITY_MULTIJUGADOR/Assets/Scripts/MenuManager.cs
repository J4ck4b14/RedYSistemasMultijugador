using System.Collections;
using UnityEngine;
using Unity.Netcode;
using UnityEngine.UI;
using TMPro;
#if UNITY_EDITOR
using UnityEditor;
#endif



/// <summary>
/// <para>Handles UI panel transitions, network startup (Host/Client/Server),
/// captures and stores the player name (limited to 16 chars),
/// and manages game-mode selection buttons.</para>
/// </summary>
public class MenuManager : MonoBehaviour
{
    private const string k_PlayerNameKey = "PlayerName";

    [Header("Panels")]
    public GameObject titlePanel;
    public GameObject networkModePanel;
    public GameObject gameModePanel;
    public GameObject lobbyPanel;

    [Header("Title Panel UI")]
    public TMP_InputField nameInput;
    public Button playButton;

    [Header("Network Mode UI")]
    public Button hostButton;
    public Button clientButton;
    public Button serverButton;

    [Header("Game Mode UI")]
    public Button freeForAllButton;
    public Button teamsButton;
    public Button captureTheFlagButton;

    [Header("General UI")]
    [Tooltip("Quit the application or stop play mode in the editor")]
    public Button quitButton;

    [Header("Lobby Manager Reference")]
    [Tooltip("Reference to the LobbyManager in the scene")]
    public LobbyManager lobbyManager;

    void Awake()
    {
        // Load saved name
        string saved = PlayerPrefs.GetString(k_PlayerNameKey, "");
        nameInput.text = saved;
        nameInput.characterLimit = 16;

        // Initial panel states
        titlePanel.SetActive(true);
        networkModePanel.SetActive(false);
        gameModePanel.SetActive(false);
        lobbyPanel.SetActive(false);

        // Hook up UI callbacks
        playButton.onClick.AddListener(OnPlayClicked);
        hostButton.onClick.AddListener(OnHostClicked);
        clientButton.onClick.AddListener(OnClientClicked);
        serverButton.onClick.AddListener(OnServerClicked);

        // Game mode buttons
        freeForAllButton.onClick.AddListener(OnFreeForAllClicked);
        teamsButton.onClick.AddListener(OnTeamsClicked);
        captureTheFlagButton.onClick.AddListener(OnCaptureTheFlagClicked);

        quitButton.onClick.AddListener(OnQuitClicked);
    }

    private IEnumerator DelayedDespawnPlayer()
    {
        yield return null; // wait one frame

        var local = NetworkManager.Singleton.SpawnManager.GetLocalPlayerObject();
        if (local != null)
            local.GetComponent<NetworkObject>().Despawn(true);
    }

    /// <summary>Validate name, save, and show network options.</summary>
    private void OnPlayClicked()
    {
        string chosen = nameInput.text.Trim();
        if (string.IsNullOrEmpty(chosen))
        {
            Debug.LogWarning("Name is required!");
            return;
        }
        PlayerPrefs.SetString(k_PlayerNameKey, chosen);
        PlayerPrefs.Save();

        networkModePanel.SetActive(true);
    }

    private void OnHostClicked()
    {
        NetworkManager.Singleton.StartHost();
        StartCoroutine(DelayedDespawnPlayer());
        titlePanel.SetActive(false);
        networkModePanel.SetActive(false);
        gameModePanel.SetActive(true);
    }

    private void OnClientClicked()
    {
        NetworkManager.Singleton.StartClient();
        StartCoroutine(DelayedDespawnPlayer());
        titlePanel.SetActive(false);
        networkModePanel.SetActive(false);
        gameModePanel.SetActive(true);
    }

    private void OnServerClicked()
    {
        NetworkManager.Singleton.StartServer();
        StartCoroutine(DelayedDespawnPlayer());
        titlePanel.SetActive(false);
        networkModePanel.SetActive(false);
        gameModePanel.SetActive(true);
    }

    /// <summary>Handle Free-For-All button click.</summary>
    private void OnFreeForAllClicked()
    {
        lobbyManager.PickMode(GameMode.FreeForAll);
        ShowLobby();
    }

    /// <summary>Handle Teams button click.</summary>
    private void OnTeamsClicked()
    {
        lobbyManager?.PickMode(GameMode.Teams);
        ShowLobby();
    }

    /// <summary>Handle Capture The Flag button click.</summary>
    private void OnCaptureTheFlagClicked()
    {
        lobbyManager?.PickMode(GameMode.CaptureTheFlag);
        ShowLobby();
    }

    /// <summary>Activate the lobby panel.</summary>
    public void ShowLobby()
    {
        titlePanel.SetActive(false);
        networkModePanel.SetActive(false);
        gameModePanel.SetActive(false);
        lobbyPanel.SetActive(true);
    }

    /// <summary>Quit application or stop playmode in Editor.</summary>
    private void OnQuitClicked()
    {
#if UNITY_EDITOR
    EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
