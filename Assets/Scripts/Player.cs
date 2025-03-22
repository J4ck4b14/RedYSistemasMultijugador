using UnityEngine;
using Unity.Netcode;

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

    // Evento que se lanza cuando se hace spawn de un objeto
    // Hay que pensar que se lanza en cada instancia de cada cliente/servidor
    //Ejercicio: Leer la nertwork variable de color, coger su valor y aplicarlo
    //Objetivo: Cuando el cliente se conecta, tiene que ver el color de los otros clientes que ya estaban cambiados
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

    public void CambiarColor(Color color)
    {
        if (IsServer)
            NetworkColor.Value = color;
        GetComponent<MeshRenderer>().material.color = color;

    }

    [ServerRpc(RequireOwnership = false)]
    public void RequestColorChangeServerRpc()
    {
        if (IsServer)
        {
            Color newColor = new Color(Random.value, Random.value, Random.value);
            NetworkColor.Value = newColor;
            ChangeColorClientRpc(newColor);
        }
    }

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
            if(IsClient)
                SubmitPositionRequestServerRpc();
        }
    }

    [ServerRpc] // Solo el servidor puede ejecutar estos metodos
    void SubmitPositionRequestServerRpc(ServerRpcParams rpcParams = default)
    {
        if(IsServer)
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