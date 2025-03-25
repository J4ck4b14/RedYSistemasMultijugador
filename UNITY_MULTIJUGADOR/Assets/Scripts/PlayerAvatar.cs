using UnityEngine;
using Unity.Netcode;
using UnityEngine.UI;
using Unity.VisualScripting;
using Unity.Netcode.Components;

[RequireComponent(typeof(NetworkTransform))]
public class PlayerAvatar : NetworkBehaviour
{
    static int INITIAL_HEALTH = 100;

    static float SHOOTING_RATE = 0.5f;

    static int BULLET_DAMAGE = 10;

    public Camera playerCamera;

    // HEALTH
    public NetworkVariable<int> currentHealth = new NetworkVariable<int>(INITIAL_HEALTH);
    public Text healthText;
    public GameObject healthBar;
    public Slider healthSlider;

    // MOVEMENT
    private CharacterController controller;
    private Vector3 playerVelocity;
    private bool groundedPlayer;
    [SerializeField]
    private float playerSpeed = 2.0f;
    private float gravityValue = -9.81f;
    [SerializeField]
    private float playerRotationSpeed = 0.25f;

    // SHOOTING
    public GameObject bulletPrefab; // Prefab of the spawneable bullet
    public Transform bulletSpawnPoint;
    private float rechargeTime = 0;
    [SerializeField]
    private int shootVelocity = 50;

    // Inicializacion
    void Start()
    {
        // Indicamos el ID del cliente en el nombre de su avatar
        this.gameObject.name = "Player" + OwnerClientId;
        controller = this.gameObject.GetComponent<CharacterController>();
        // Actualizamos la interfaz en el callback de salud
        currentHealth.OnValueChanged += OnHealthChange;
        // Cuando no es nuestro avatar, desactivamos la interfaz y la camara
        if(!IsLocalPlayer)
        {
            playerCamera.gameObject.SetActive(false);
            healthText.gameObject.SetActive(false);
        }
    }

    private void Update()
    {
        // El servidor tiene que realizar sus calculos para los jugadores
        if(IsServer)
        {
            // El servidor calcula el tiempo que queda para disparar
            if(rechargeTime > 0)
            {
                rechargeTime -= Time.deltaTime;
            }
        }

        // Si no es el avatar local, solo actualizar la barrada de vida
        if (!IsLocalPlayer)
        {
            // Hacemos que la barra de vida mire hacia el jugador local
            UpdateHealthBar();
            return;
        }

        // Si es el avatar del jugador local
        UpdateInput();
    }

    // Comprueba input que se tenga que mandar al servidor
    private void UpdateInput()
    {
        float axisH = Input.GetAxis("Horizontal");
        float axisV = Input.GetAxis("Vertical");
        bool shootPressed = Input.GetMouseButtonDown(0);
        UpdatePlayerServerRpc(axisH, axisV, shootPressed);
    }

    // Cuando la vida cambia, actualizamos la interfaz
    private void OnHealthChange(int prevHealth, int newHealth)
    {
        // Si es el jugador local, actualizamos la interfaz 
        
        // Si es otro jugador, actualizamos su barra de vida
    }

    // Orienta la barra de vida al jugador principal
    private void UpdateHealthBar()
    {
        if (healthBar != null)
            healthBar.transform.LookAt(NetworkManager.Singleton.SpawnManager.GetLocalPlayerObject().gameObject.transform);
    }

    // Mandamos el input del jugador al servidor
    [ServerRpc]
    void UpdatePlayerServerRpc(float axisH, float axisV, bool shootPressed)
    {
        groundedPlayer = controller.isGrounded;
        if (groundedPlayer && playerVelocity.y < 0)
        {
            playerVelocity.y = 0f;
        }

        Vector3 moveH = axisH * transform.right;
        Vector3 moveV = axisV * transform.forward;
        Vector3 moveDirection = moveH + moveV;
        moveDirection.Normalize();
        controller.Move(moveV * playerSpeed * Time.deltaTime);

        transform.rotation = Quaternion.Euler(new Vector3(0f, axisH * playerRotationSpeed, 0f)) * transform.rotation;

        playerVelocity.y += gravityValue * Time.deltaTime;
        controller.Move(playerVelocity * Time.deltaTime);

        // La actualizacion a los clientes se realiza con NetworkTransform

        if (shootPressed)
            Shoot();
    }

    // Solo el servidor puede dispara para generar la bala
    private void Shoot()
    {
        if (!IsServer)
            return;

        if(rechargeTime <= 0)
        {
            rechargeTime = SHOOTING_RATE;
            // Spawn bullet and add movement
        }
    }

    // Solo el servidor debe poder quitar vida
    public void DamagePlayer()
    {
        if(IsServer)
            currentHealth.Value -= BULLET_DAMAGE;
    }
}
