using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(NetworkObject))]
public class Player : NetworkBehaviour
{
    // Informacion que queremos sincronizar
    public NetworkVariable<Vector3> Position = new NetworkVariable<Vector3>();
    // 1º Variable que sincronnizar
    public NetworkVariable<int> NetworkSalud = new NetworkVariable<int>();
    //Variable tipo color Sincronizar
    public NetworkVariable<Color> NetworkColor = new NetworkVariable<Color>();
    private TMPro.TMP_Text textoVida;

    /// <summary>
    /// <para></para>
    /// Method called when the NetworkObject associated with this script is spawned on the network.<br />
    /// It's called after Unity's Start method for objects managed by Unity Netcode.<br />
    /// HERE'S where you initialize network-related logic, like subscribing to NetworkVariable callbacks.<br />
    /// <br />
    /// For more information, visit:<br />
    /// https://docs-multiplayer.unity3d.com/netcode/current/basics/networkbehavior/ <br />
    /// </summary>
    public override void OnNetworkSpawn()
    {
        // Comprobamos si somos el propietario
        if (IsOwner)
        {
            Debug.Log(IsLocalPlayer);
            Move();

            // Busco la interfaz de vida
            var vidaPlayer = GameObject.Find("VidaPlayer");
            textoVida = vidaPlayer.GetComponent<TMPro.TMP_Text>();
            textoVida.text = NetworkSalud.Value.ToString();

            // 4º Suscribir callback si soy el propietario
            NetworkSalud.OnValueChanged += CambioVida;
            GetComponent<MeshRenderer>().material.color = NetworkColor.Value;
        }

        // Apply the current color when spawned
        CambiarColor(NetworkColor.Value);

        // Subscribe to color changes for all instancess
        NetworkColor.OnValueChanged += CambioColor;
    }

    // 3º Metodo callback cuando cambia la vida
    public void CambioVida(int vidaAnterior, int nuevaVida)
    {
        Debug.Log("Vida went from " + vidaAnterior + " to " + nuevaVida);
        // Tiene que hacer el cambio que ve el cliente
        textoVida.text = nuevaVida.ToString();
    }

    /// <summary>
    /// <para></para>
    /// This method is triggered whenever the value of the NetworkColor variable changes, thanks to the OnValueChanged event,<br />
    /// which ensures all connected clients automatically receive the updated value, which triggers this callback.<br />
    /// It ensures that all clients update their local representation of the player's color.<br />
    /// </summary>
    /// <param name="colorAnterior"></param>
    /// <param name="colorNuevo"></param>
    public void CambioColor(Color colorAnterior, Color colorNuevo)
    {
        Debug.Log("Color went from " + colorAnterior + " to " + colorNuevo);
        // Tiene que hacer el cambio que ve el cliente
        CambiarColor(colorNuevo);

    }

    // 2º Un metodo que cambie la variable
    public void QuitarVida(int cantidad)
    {
        if (IsServer)
            NetworkSalud.Value -= cantidad; // 6º Si esto lo hace el servidor, llama al callback en los clientes
    }

    /// <summary>
    /// <para></para>
    /// Helper method.<br />
    /// Directly applies a given color to the player's material.<br />
    /// Centralizes color application logic to avoid redundancy.<br />
    /// </summary>
    /// <param name="color"></param>
    public void CambiarColor(Color color)
    {
        if (IsServer)
            NetworkColor.Value = color;
        GetComponent<MeshRenderer>().material.color = color;

    }

    /// <summary>
    /// Remote Procedure Call method. Allows a client to send request to the server.
    /// Executed on the server; initiated by any client.<br />
    /// For more information: https://docs-multiplayer.unity3d.com/netcode/current/basics/networkbehavior/<br />
    /// <br />
    /// <para>This method creates a random color and forwards it to the ChangeColorClientRpc method.</para>
    /// </summary>
    [ServerRpc(RequireOwnership = false)]
    public void RequestColorChangeServerRpc(Color desiredColor)
    {
        if (IsServer)
        {
            if (desiredColor == new Color(0,0,0,0))
            {
                Color newColor = new Color(Random.value, Random.value, Random.value);
                NetworkColor.Value = newColor;
                ChangeColorClientRpc(newColor);

            }
            else
            {
                NetworkColor.Value = desiredColor;
                ChangeColorClientRpc(desiredColor);
            }
        }
    }

    /// <summary>
    /// Remote Procedure Call method. Allows the server to send instructions to all connected clients.<br />
    /// For more information: https://docs-multiplayer.unity3d.com/netcode/current/basics/networkbehavior/<br />
    /// <br />
    /// This method calls the CambiarColor method and forwards the color it was provided.<br />
    /// </summary>
    [ClientRpc]
    private void ChangeColorClientRpc(Color newColor)
    {
        CambiarColor(newColor);
    }

    // Funcion que mueve al jugador, solo puede ser ejecutada por el servidor
    public void Move()
    {
        // Si somos el servidor, movemos a nuestra instancia de jugador a una posicion aleatoria
        if (NetworkManager.Singleton.IsServer)
        {
            var randomPosition = GetRandomPositionOnPlane();
            transform.position = randomPosition;
            Position.Value = randomPosition;
            ChangeColorRequestClientRpc();
        }
        // Si no somos el servidor, podemos pedir a este que haga el movimiento
        else
        {
            if (IsClient)
                SubmitPositionRequestServerRpc();
        }
    }

    [ServerRpc] // Solo el servidor puede ejecutar estos metodos
    void SubmitPositionRequestServerRpc(ServerRpcParams rpcParams = default)
    {
        if (IsServer)
            Position.Value = GetRandomPositionOnPlane();
    }
    [ClientRpc]
    void ChangeColorRequestClientRpc()
    {
        if (IsClient)
        {

        }
    }
    // Metodo que devuelve una posicion aleatoria dentro de un rango en el plano XZ
    static Vector3 GetRandomPositionOnPlane()
    {
        return new Vector3(Random.Range(-3f, 3f), 1f, Random.Range(-3f, 3f));
    }

    // Actualizamos la posicion de esta instancia con la variable sincronizada
    void Update()
    {
        transform.position = Position.Value;
    }
}