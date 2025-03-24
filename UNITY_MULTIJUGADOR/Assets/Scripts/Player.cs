using UnityEngine;
using Unity.Netcode;

[RequireComponent(typeof(NetworkObject))]
public class Player : NetworkBehaviour
{
    // Informacion que queremos sincronizar
    public NetworkVariable<Vector3> Position = new NetworkVariable<Vector3>();

    // Evento que se lanza cuando se hace spawn de un objeto
    // Hay que pensar que se lanza en cada instancia de cada cliente/servidor
    public override void OnNetworkSpawn()
    { 
        // Comprobamos si somos el propietario
        if (IsOwner)
        {
            Debug.Log(IsLocalPlayer);
            Move(); 
        }
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
        }
        // Si no somos el servidor, podemos pedir a este que haga el movimiento
        else
        {
            SubmitPositionRequestServerRpc();
        }
    }

    [ServerRpc] // Solo el servidor puede ejecutar estos metodos
    void SubmitPositionRequestServerRpc(ServerRpcParams rpcParams = default)
    {
        Position.Value = GetRandomPositionOnPlane();
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