using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// <para>Networked player controller that handles movement, shooting, health, and visual representation.</para>
/// This bad boy does everything - from shooting bullets to taking damage to looking pretty...  He tries, OK?
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

    #region Variables
    #region Constants
    private const int INITIAL_HEALTH = 100;
    private const float SHOOTING_RATE = 0.5f;
    private const int BULLET_DAMAGE = 10;
    private const int MAX_PLAYERS = 4;
    private static readonly Color DEAD_COLOR = Color.gray;
    #endregion

    #region Network Variables
    [Header("Network Variables")]

    /// <summary>Synchronized across network. Represents player buffs</summary>
    public NetworkVariable<bool> hasSpeedBuff = new NetworkVariable<bool>(false);
    public NetworkVariable<bool> hasDamageBuff = new NetworkVariable<bool>(false);
    public NetworkVariable<float> buffEndTime = new NetworkVariable<float>(0f);
    /// <summary>Synchronized across network. Represents current player health (0-100)</summary>
    [Tooltip("Current player health - syncs across network")]
    public NetworkVariable<int> currentHealth = new NetworkVariable<int>(INITIAL_HEALTH);

    /// <summary>Synchronized across network. Determines player visual color</summary>
    [Tooltip("Player color - changes based on player ID")]
    public NetworkVariable<Color> playerColor = new NetworkVariable<Color>();

    /// <summary>Synchronized across network. Tracks if player is dead</summary>
    [Tooltip("Are we dead? Syncs so everyone knows we're toast")]
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
    private RotatingDoor nearbyRotatingDoor;
    private ElevatorDoor nearbyElevatorDoor;
    #endregion

    #region Power Up System
    [Header("Power Up Variables")]
    public float bulletDamageMultiplier = 1.0f; // sincronizado con hasDamageBuff
    private Coroutine powerUpUICoroutine;
    #endregion

    #region Shooting System
    [Header("Shooting System")]
    [SerializeField] private GameObject bulletPrefab;
    [SerializeField] private Transform bulletSpawnPoint;
    [SerializeField] private int shootVelocity = 50;
    private float serverCooldownEndTime = 0f;
    private float localCooldownEndTime = 0f;
    #endregion

    #region UI Elements
    [Header("UI Elements")]
    public Camera playerCamera;
    public Text healthText;
    public Text powerUpText;
    public GameObject healthBar;
    public Slider healthSlider;
    public Slider reloadBar;
    public Slider powerUpBar;
    // Referencia al slider visual sobre el jugador (debe conectarse en el inspector)
    public Slider buffSlider;
    public Image reloadFill;
    public Image powerUpFill;
    public Image buffSliderFill;
    public Text killFeedText;
    private const int MAX_KILL_MESSAGES = 5;
    private readonly Queue<string> killMessages = new Queue<string>();

    private static Transform localCameraTransform;

    #endregion
    #endregion

    /* -------------------------------------------------------------------------- */
    /*                                 LIFECYCLE                                  */
    /* -------------------------------------------------------------------------- */

    #region Unity Callbacks
    /// <summary>
    /// Called when player spawns - initializes all the important shiiii
    /// </summary>
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        InitializePlayerIdentity();
        SetupNetworkCallbacks();

        // Server-only setup
        if (IsServer)
        {
            TeleportToSpawnPoint();
            AssignUniqueColor();
        }
        // Server-only setup
        if (IsServer)
        {
            TeleportToSpawnPoint();
            AssignUniqueColor();
        }

        ConfigurePlayerUI();
    }

    /// <summary>
    /// Runs every frame - handles input and UI updates
    /// </summary>
    private void Update()
    {
        if (isDead.Value)
        {
            return;
        }

        UpdateBuffState();  // Run buff logic on ALL instances

        if (!IsLocalPlayer)
        {
            UpdateRemotePlayerUI(); // Keep remote health/buff facing camera
            return;
        }

        UpdateReloadUI();
        HandleInput();
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

    /* -------------------------------------------------------------------------- */
    /*                               PLAYER SYSTEMS                               */
    /* -------------------------------------------------------------------------- */
    #region Player System

    #region Input Handling
    /// <summary>
    /// Handles player input (local player only)
    /// </summary>
    private void HandleInput()
    {
        float axisH = Input.GetAxis("Horizontal");
        float axisV = Input.GetAxis("Vertical");
        bool shootPressed = Input.GetMouseButton(0);

        if (Input.GetKeyDown(KeyCode.E))
        {
            if (nearbyRotatingDoor != null)
            {
                nearbyRotatingDoor.TryInteractServerRpc(); // Call server to rotate door
            }
            else if (nearbyElevatorDoor != null)
            {
                Debug.Log("[CLIENT] Pressed E near elevator door: " + nearbyElevatorDoor.name);
                nearbyElevatorDoor.TryOpenFromPlayerServerRpc(); // Elevator handles logic itself
            }
        }

        // Client-side prediction
        if (shootPressed && Time.time >= localCooldownEndTime)
        {
            localCooldownEndTime = Time.time + SHOOTING_RATE;
        }

        UpdatePlayerServerRpc(axisH, axisV, shootPressed);
    }

    [ServerRpc]
    private void UpdatePlayerServerRpc(float axisH, float axisV, bool shootPressed)
    {
        if (isDead.Value) return;

        HandleMovement(axisH, axisV);  // Movement handling

        if (shootPressed && Time.time >= serverCooldownEndTime)  // Shooting with cooldown
        {
            Shoot();
            serverCooldownEndTime = Time.time + SHOOTING_RATE;

            if (IsOwner)  // Sync with client prediction
            {
                localCooldownEndTime = serverCooldownEndTime;
            }
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
    private void Shoot()
    {
        GameObject bullet = Instantiate(bulletPrefab, bulletSpawnPoint.position, bulletSpawnPoint.rotation);
        bullet.GetComponent<NetworkObject>().SpawnWithOwnership(OwnerClientId);

        // Set bullet properties
        Bullet bulletScript = bullet.GetComponent<Bullet>();
        bulletScript.bulletColor.Value = playerColor.Value;

        // Apply velocity
        bullet.GetComponent<Rigidbody>().linearVelocity = bulletSpawnPoint.forward * shootVelocity;
    }
    #endregion

    #region Health System
    /// <summary>
    /// Applies damage to player (server only)
    /// </summary>
    public void DamagePlayer(ulong damagerId) // MODIFIED TO ACCEPT DAMAGER ID
    {
        if (!IsServer || isDead.Value) return;
        PlayerAvatar attacker = NetworkManager.Singleton.ConnectedClients[damagerId]
        .PlayerObject.GetComponent<PlayerAvatar>();

        int finalDamage = Mathf.RoundToInt(BULLET_DAMAGE * attacker.bulletDamageMultiplier); // Apply damage multiplier
        currentHealth.Value -= finalDamage;

        if (currentHealth.Value <= 0)
        {
            HandlePlayerDeath(damagerId); // PASS KILLER ID
        }
    }

    /// <summary>
    /// Handles player death (server only)
    /// </summary>
    private void HandlePlayerDeath(ulong killerId)
    {
        isDead.Value = true;
        playerColor.Value = DEAD_COLOR;

        // Get killer's color
        Color killerColor = NetworkManager.Singleton.ConnectedClients[killerId]
            .PlayerObject.GetComponent<PlayerAvatar>().playerColor.Value;

        // Format kill message
        string message = FormatKillMessage(killerId, killerColor);

        // Send to server to broadcast
        ReportKillServerRpc(message);
    }

    [ServerRpc]
    private void ReportKillServerRpc(string message)
    {
        BroadcastKillClientRpc(message);
    }

    [ClientRpc]
    private void BroadcastKillClientRpc(string message)
    {
        AddKillMessage(message);
    }

    private string FormatKillMessage(ulong killerId, Color killerColor)
    {
        string killerHex = ColorUtility.ToHtmlStringRGB(killerColor);
        string victimHex = ColorUtility.ToHtmlStringRGB(playerColor.Value);
        return $"<color=#{killerHex}>Player {killerId}</color> killed <color=#{victimHex}>Player {OwnerClientId}</color>";
    }

    private void AddKillMessage(string message)
    {
        if (!IsLocalPlayer) return; // Only update local player's kill feed

        killMessages.Enqueue(message);
        if (killMessages.Count > MAX_KILL_MESSAGES)
        {
            killMessages.Dequeue();
        }

        UpdateKillFeedDisplay();
    }

    private void UpdateKillFeedDisplay()
    {
        killFeedText.text = string.Join("\n", killMessages);
    }
    #endregion

    #region Door System

    /// <summary>
    /// Called by a door when the player enters its trigger
    /// </summary>
    public void SetNearbyRotatingDoor(RotatingDoor door)
    {
        nearbyRotatingDoor = door;
    }

    public void ClearNearbyRotatingDoor(RotatingDoor door)
    {
        if (nearbyRotatingDoor == door)
        {
            nearbyRotatingDoor = null;
        }
    }

    public void SetNearbyElevatorDoor(ElevatorDoor door)
    {
        nearbyElevatorDoor = door;
    }

    public void ClearNearbyElevatorDoor(ElevatorDoor door)
    {
        if (nearbyElevatorDoor == door)
        {
            nearbyElevatorDoor = null;
        }
    }

    #endregion
    #endregion

    /* -------------------------------------------------------------------------- */
    /*                                    UI                                      */
    /* -------------------------------------------------------------------------- */

    #region UI System
    /// <summary>
    /// Set up UI for local player (first person view)
    /// </summary>
    private void SetupLocalPlayerUI()
    {
        // Ensure only the local player's camera is active
        if (playerCamera != null)
        {
            playerCamera.gameObject.SetActive(true);

            // Local UI (screen-space canvas)
            if (healthText != null)
            {
                healthText.gameObject.SetActive(true);
                healthText.text = $"HP: {currentHealth.Value}";
            }

            if (reloadBar != null)
            {
                reloadBar.gameObject.SetActive(true);
            }

            // Remember this player's camera for everybody else's facing logic
            if (playerCamera != null)
            {
                localCameraTransform = playerCamera.transform;
            }
        }

        // Disable world-space UI meant for others
        if (healthBar != null)
        {
            healthBar.SetActive(false);
        }
        playerCamera.gameObject.SetActive(true);
        healthText.gameObject.SetActive(true);
        healthText.text = $"HP: {currentHealth.Value}";

        if (healthSlider != null)
        {
            healthSlider.gameObject.SetActive(false);
        }

        killFeedText.gameObject.SetActive(true);
        killFeedText.text = "";

        // Hide the local power-up bar at game start
        if (powerUpBar != null)
        {
            powerUpBar.gameObject.SetActive(false);
            powerUpBar.value = 0f;
        }

        // Ensure the buff slider is hidden and reset at spawn
        if (buffSlider != null)
        {
            buffSlider.value = 0f;            // Start at zero
            buffSlider.gameObject.SetActive(false); // Hide until buff activates
        }
    }

    /// <summary>
    /// Set up UI for remote players (third person view)
    /// </summary>
    private void SetupRemotePlayerUI()
    {
        // Disable first-person UI
        if (playerCamera != null)
        {
            playerCamera.gameObject.SetActive(false);
        }

        if (healthText != null)
        {
            healthText.gameObject.SetActive(false);
        }

        if (reloadBar != null)
        {
            reloadBar.gameObject.SetActive(false);
        }

        // Enable world-space health bar
        if (healthBar != null)
        {
            healthBar.SetActive(true);
            healthSlider.value = currentHealth.Value / (float)INITIAL_HEALTH;
        }

        // Hide their little world-space buff slider until *they* get a buff
        if (buffSlider != null)
        {
            buffSlider.gameObject.SetActive(false);
            buffSlider.value = 0f;
        }

        // Also ensure the local-only bar never shows on remote avatars
        if (powerUpBar != null)
        {
            powerUpBar.gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// Makes health bars face the local player's camera
    /// </summary>
    private void UpdateRemotePlayerUI()
    {
        if (Camera.main != null)
        {
            // Health bar
            if (healthBar != null)
            {
                Vector3 dir = localCameraTransform.position - healthBar.transform.position;
                dir.y = 0;
                healthBar.transform.rotation = Quaternion.LookRotation(dir);
            }

            // Buff slider
            if (buffSlider != null)
            {
                Vector3 dir2 = localCameraTransform.position - buffSlider.transform.position;
                dir2.y = 0;
                buffSlider.transform.rotation = Quaternion.LookRotation(dir2);
            }
        }

        killFeedText.gameObject.SetActive(false);
    }

    /// <summary>
    /// Make the reload bar go brrrr
    /// </summary>
    private void UpdateReloadUI()
    {
        if (reloadBar == null) return;

        float progress = Mathf.Clamp01(1 - ((localCooldownEndTime - Time.time) / SHOOTING_RATE));
        reloadBar.value = progress;

        if (reloadFill != null)
        {
            reloadFill.color = Color.Lerp(Color.red, Color.green, progress);
        }
    }

    private void UpdateRemotePowerUpUI()
    {
        if (buffSlider != null && Camera.main != null)
        {
            // Make the slider always face the camera
            buffSlider.transform.LookAt(Camera.main.transform);
        }
    }

    #endregion

    /* -------------------------------------------------------------------------- */
    /*                               NETWORK CALLBACKS                            */
    /* -------------------------------------------------------------------------- */

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
        else
        {
            if (healthSlider != null)
            {
                healthSlider.value = current / (float)INITIAL_HEALTH;
            }

            // Update the power-up slider for remote players
            UpdateRemotePowerUpUI();
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

    #region Power Up System
    // Llamado desde PowerUp.cs cuando el jugador toca un power-up
    // Es llamado desde el propio cliente que recogió el Power-Up
    // Esto garantiza que tenga ownership y el mensaje se procese correctamente
    [ServerRpc]
    public void SendPowerUpToServerRpc(PowerUpType type)
    {
        Debug.Log($"[SERVER] PowerUp recibido: {type} de {OwnerClientId}");

        // Apply buffs based on the power-up type
        switch (type)
        {
            case PowerUpType.HP:
                currentHealth.Value = INITIAL_HEALTH;
                break;

            case PowerUpType.PowerBullet:
                hasDamageBuff.Value = true;
                bulletDamageMultiplier = 1.5f;
                buffEndTime.Value = Time.time + 30f;
                break;

            case PowerUpType.SpeedCola:
                hasSpeedBuff.Value = true;
                playerSpeed = 40.0f; // Cambia si tu velocidad base es diferente
                buffEndTime.Value = Time.time + 30f;
                break;
        }

        // Update the local player's UI elements for buffs
        if (IsLocalPlayer)
        {
            ShowPowerUpUI(type, 30f);

            if (buffSlider != null)
            {
                buffSlider.gameObject.SetActive(true);
                buffSlider.maxValue = 30f;
                buffSlider.value = 30f;      // Jumps to full duration, only activated when the power-up is taken
            }
        }
    }

    // Verifica si el buff terminó (llamar desde Update)
    private void UpdateBuffState()
    {
        // Calcula el tiempo restante del buff en base a Time.time
        float timeLeft = Mathf.Clamp01((buffEndTime.Value - Time.time) / 30);

        // Si hay algún buff activo
        if (hasSpeedBuff.Value || hasDamageBuff.Value)
        {
            // World-space bar for THIS avatar (local or remote)
            if (buffSlider != null)
                buffSlider.gameObject.SetActive(true);

            // Local UI only if YOU are the buffed owner
            if (IsLocalPlayer && powerUpBar != null)
                powerUpBar.gameObject.SetActive(true);

            // Actualiza las barras visuales
            if (buffSlider != null && buffSliderFill != null)
            {
                buffSlider.value = timeLeft; // Slider exterior (lo que ven otro player)
                buffSliderFill.color = Color.Lerp(Color.red, Color.green, timeLeft);
            }

            // Update the power-up bar for the local player
            if (powerUpBar != null && powerUpFill != null)
            {
                powerUpBar.value = timeLeft; // Slider local en el canvas
                powerUpFill.color = Color.Lerp(Color.red, Color.green, timeLeft);
            }

            // Si se acabó el buff, avisa al servidor
            if (timeLeft <= 0f)
            {
                EndBuffServerRpc(); // Solo el dueño pide al server que termine el buff
            }
        }
        else
        {
            // Hide world-space when THIS avatar has no buff
            if (buffSlider != null)
                buffSlider.gameObject.SetActive(false);

            // Hide local bar when YOU have no buff
            if (IsLocalPlayer && powerUpBar != null)
                powerUpBar.gameObject.SetActive(false);
        }
    }
    [ServerRpc]
    private void EndBuffServerRpc()
    {
        hasSpeedBuff.Value = false;
        hasDamageBuff.Value = false;
        playerSpeed = 20.0f; // vuelve a la velocidad normal
        bulletDamageMultiplier = 1.0f;
    }
    // Llama esto cuando se activa un buff
    private void ShowPowerUpUI(PowerUpType type, float duration)
    {
        if (!IsLocalPlayer || powerUpText == null) return;
        string message = type switch
        {
            PowerUpType.PowerBullet => $"¡Balas Potenciadas! ({duration}s)",
            PowerUpType.SpeedCola => $"¡Velocidad Aumentada! ({duration}s)",
            _ => string.Empty
        };

        if (powerUpUICoroutine != null)
            StopCoroutine(powerUpUICoroutine);

        powerUpUICoroutine = StartCoroutine(DisplayPowerUpMessage(message, duration));
    }

    // Corrutina que muestra el mensaje durante la duración del buff
    private IEnumerator DisplayPowerUpMessage(string message, float duration)
    {
        powerUpText.text = message;
        powerUpText.gameObject.SetActive(true);
        yield return new WaitForSeconds(duration);
        powerUpText.gameObject.SetActive(false);
    }
    #endregion
}