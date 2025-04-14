using UnityEngine;
using Unity.Netcode;
using UnityEngine.UI;
using Unity.Netcode.Components;

/// <summary>
/// <para>Networked player avatar handling movement, shooting, health, and visual representation.</para>
/// This class manages all player-related gameplay systems and synchronizes them across the network.
/// </summary>
[RequireComponent(typeof(NetworkTransform))]
public class PlayerAvatar : NetworkBehaviour
{
    #region Comments & Future updates
    /*
     * How cool would it be if we had a text in the player to upload info EVERY-shooter-game style?
     * 
     */
    #endregion

    #region Constants
    private const int INITIAL_HEALTH = 100;
    private const float SHOOTING_RATE = 0.5f;
    private const int BULLET_DAMAGE = 10;
    private const int MAX_PLAYERS = 4;
    private static readonly Color DEAD_COLOR = Color.gray;
    #endregion

    #region Network Variables
    /// <summary>Synchronized across network. Represents current player health (0-100)</summary>
    public NetworkVariable<int> currentHealth = new NetworkVariable<int>(INITIAL_HEALTH);

    /// <summary>Synchronized across network. Determines player visual color</summary>
    public NetworkVariable<Color> playerColor = new NetworkVariable<Color>();

    /// <summary>Synchronized across network. Tracks if player is dead</summary>
    public NetworkVariable<bool> isDead = new NetworkVariable<bool>(false);
    #endregion

    #region Player Components
    [Header("Player Components")]
    [SerializeField] private CharacterController controller;
    private Material playerMaterial;
    #endregion

    #region Movement Settings
    [Header("Movement Settings")]
    [SerializeField] private float playerSpeed = 2.0f;
    [SerializeField] private float playerRotationSpeed = 0.25f;
    private readonly float gravityValue = -9.81f;
    private Vector3 playerVelocity;
    private bool groundedPlayer;
    #endregion

    #region Shooting System
    [Header("Shooting System")]
    [SerializeField] private GameObject bulletPrefab;
    [SerializeField] private Transform bulletSpawnPoint;
    [SerializeField] private int shootVelocity = 50;
    private float rechargeTime = 0;
    #endregion

    #region UI Elements
    [Header("UI Elements")]
    public Camera playerCamera;
    public Text healthText;
    public GameObject healthBar;
    public Slider healthSlider;
    public Slider reloadBar;
    #endregion

    #region Unity Callbacks
    /// <summary>
    /// Initializes player when spawned on network
    /// </summary>
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        InitializePlayerIdentity();
        SetupNetworkCallbacks();

        if (IsServer)
        {
            InitializeServerPlayer();
        }

        ConfigurePlayerUI();
    }

    /// <summary>
    /// Handles physics updates
    /// </summary>
    private void FixedUpdate()
    {
        if (isDead.Value) return;

        if (IsServer)
        {
            UpdateShootingCooldown();
        }

        if (IsLocalPlayer)
        {
            UpdateInput();
        }
        else
        {
            UpdateRemotePlayerUI();
        }
    }
    #endregion

    #region Initialization
    /// <summary>
    /// Sets up player identity and visual representation
    /// </summary>
    private void InitializePlayerIdentity()
    {
        playerMaterial = GetComponentInChildren<Renderer>().material;
        playerMaterial.color = playerColor.Value;
        gameObject.name = $"Player_{OwnerClientId}";
        controller = GetComponent<CharacterController>();
    }

    /// <summary>
    /// Server-only initialization
    /// </summary>
    private void InitializeServerPlayer()
    {
        TeleportToSpawnPoint();
        AssignUniqueColor();
    }

    /// <summary>
    /// Sets up all network variable change callbacks
    /// </summary>
    private void SetupNetworkCallbacks()
    {
        playerColor.OnValueChanged += OnColorChanged;
        currentHealth.OnValueChanged += OnHealthChanged;
        isDead.OnValueChanged += OnDeathStateChanged;
    }

    /// <summary>
    /// Configures UI based on player type (local or remote)
    /// </summary>
    private void ConfigurePlayerUI()
    {
        if (IsLocalPlayer)
        {
            SetupLocalPlayerUI();
        }
        else
        {
            SetupRemotePlayerUI();
        }
    }
    #endregion

    #region Spawn System
    /// <summary>
    /// Teleports player to an available spawn point (server only)
    /// </summary>
    private void TeleportToSpawnPoint()
    {
        GameObject[] spawns = GameObject.FindGameObjectsWithTag("Spawnpoint");
        if (spawns.Length == 0) return;

        int spawnIndex = (int)OwnerClientId % spawns.Length;
        transform.position = spawns[spawnIndex].transform.position;
        transform.rotation = spawns[spawnIndex].transform.rotation;
    }

    /// <summary>
    /// Assigns a unique color to each player (server only)
    /// </summary>
    private void AssignUniqueColor()
    {
        float hue = (float)OwnerClientId / MAX_PLAYERS;
        playerColor.Value = Color.HSVToRGB(hue, 0.8f, 0.8f);
    }
    #endregion

    #region Input Handling
    /// <summary>
    /// Handles player input (local player only)
    /// </summary>
    private void UpdateInput()
    {
        float axisH = Input.GetAxis("Horizontal");
        float axisV = Input.GetAxis("Vertical");
        bool shootPressed = Input.GetMouseButtonDown(0);

        UpdatePlayerServerRpc(axisH, axisV, shootPressed);
    }

    /// <summary>
    /// Server RPC to update player movement and handle shooting
    /// </summary>
    [ServerRpc]
    private void UpdatePlayerServerRpc(float axisH, float axisV, bool shootPressed)
    {
        if (isDead.Value) return;

        HandleMovement(axisH, axisV);

        if (shootPressed)
        {
            AttemptShoot();
        }
    }
    #endregion

    #region Movement System
    /// <summary>
    /// Handles player movement physics
    /// </summary>
    private void HandleMovement(float axisH, float axisV)
    {
        groundedPlayer = controller.isGrounded;
        if (groundedPlayer && playerVelocity.y < 0)
        {
            playerVelocity.y = 0f;
        }

        Vector3 move = new Vector3(0, 0, axisV);
        move = transform.TransformDirection(move);
        controller.Move(move * (playerSpeed * Time.deltaTime));

        if (axisH != 0)
        {
            transform.Rotate(0, axisH * playerRotationSpeed, 0);
        }

        playerVelocity.y += gravityValue * Time.deltaTime;
        controller.Move(playerVelocity * Time.deltaTime);
    }
    #endregion

    #region Shooting System
    /// <summary>
    /// Updates shooting cooldown timer (server only)
    /// </summary>
    private void UpdateShootingCooldown()
    {
        if (rechargeTime > 0)
        {
            rechargeTime -= Time.deltaTime;
            reloadBar.value = Mathf.Clamp01(1-(rechargeTime/SHOOTING_RATE));
        }
        else
        { reloadBar.value = 1f; }
    }

    /// <summary>
    /// Attempts to shoot if cooldown allows (server only)
    /// </summary>
    private void AttemptShoot()
    {
        if (rechargeTime <= 0)
        {
            Shoot();
            rechargeTime = SHOOTING_RATE;
        }
    }

    /// <summary>
    /// Creates and launches a bullet (server only)
    /// </summary>
    private void Shoot()
    {
        GameObject bullet = Instantiate(bulletPrefab, bulletSpawnPoint.position, bulletSpawnPoint.rotation);
        NetworkObject bulletNetworkObject = bullet.GetComponent<NetworkObject>();
        bulletNetworkObject.SpawnWithOwnership(OwnerClientId);

        Bullet bulletScript = bullet.GetComponent<Bullet>();
        bulletScript.bulletColor.Value = playerColor.Value;

        Rigidbody rb = bullet.GetComponent<Rigidbody>();
        rb.linearVelocity = bulletSpawnPoint.forward * shootVelocity;
    }
    #endregion

    #region Health System
    /// <summary>
    /// Applies damage to player (server only)
    /// </summary>
    public void DamagePlayer()
    {
        if (!IsServer || isDead.Value) return;

        currentHealth.Value = Mathf.Max(0, currentHealth.Value - BULLET_DAMAGE);

        if (currentHealth.Value <= 0)
        {
            HandlePlayerDeath();
        }
    }

    /// <summary>
    /// Handles player death (server only)
    /// </summary>
    private void HandlePlayerDeath()
    {
        isDead.Value = true;
        playerColor.Value = DEAD_COLOR;
    }
    #endregion

    #region UI System
    /// <summary>
    /// Sets up UI for local player
    /// </summary>
    private void SetupLocalPlayerUI()
    {
        playerCamera.gameObject.SetActive(true);
        healthText.gameObject.SetActive(true);
        healthText.text = $"HP: {currentHealth.Value}";

        if (healthSlider != null)
        {
            healthSlider.gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// Sets up UI for remote players
    /// </summary>
    private void SetupRemotePlayerUI()
    {
        playerCamera.gameObject.SetActive(false);
        healthText.gameObject.SetActive(false);

        if (healthSlider != null)
        {
            healthSlider.value = currentHealth.Value / (float)INITIAL_HEALTH;
        }
    }

    /// <summary>
    /// Updates remote player UI elements
    /// </summary>
    private void UpdateRemotePlayerUI()
    {
        if (healthBar != null)
        {
            healthBar.transform.LookAt(Camera.main.transform);
        }
    }
    #endregion

    #region Network Callbacks
    /// <summary>
    /// Called when player color changes
    /// </summary>
    private void OnColorChanged(Color previous, Color current)
    {
        playerMaterial.color = current;
    }

    /// <summary>
    /// Called when health changes
    /// </summary>
    private void OnHealthChanged(int previous, int current)
    {
        if (IsLocalPlayer)
        {
            healthText.text = $"HP: {current}";
        }
        else if (healthSlider != null)
        {
            healthSlider.value = current / (float)INITIAL_HEALTH;
        }
    }

    /// <summary>
    /// Called when death state changes
    /// </summary>
    private void OnDeathStateChanged(bool previous, bool current)
    {
        if (current && IsLocalPlayer)
        {
            healthText.text = "DEAD";
        }
    }
    #endregion
}