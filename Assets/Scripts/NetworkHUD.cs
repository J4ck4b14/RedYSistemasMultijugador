using UnityEngine;
using Unity.Netcode;

public class NetworkHUD : MonoBehaviour
{
    void OnGUI()
    {
        // Creamos una interfaz en pantalla
        GUILayout.BeginArea(new Rect(10, 10, 300, 300));
        // Si no somo un cliente o un servidor
        if (!NetworkManager.Singleton.IsClient && !NetworkManager.Singleton.IsServer)
        {
            // Mostramos los botones para iniciar una conexion
            StartButtons();
        }
        else
        {
            // Mostramos el estado del multijugador
            StatusLabels();
            // Si queremos pedir una nueva posicion
            SubmitNewPosition();
            // Si soy el servidor, mostrar boton
            if(NetworkManager.Singleton.IsServer)
            {
                if (GUILayout.Button("Quitar vida")) // Muestra un boton con el texto azul
                {
                    // Se ejecuta cuando se pulsa el boton
                    var players = GameObject.FindObjectsOfType<Player>(); // Devuelve un array con los componentes
                    var playersGameObjects = GameObject.FindGameObjectsWithTag("Player"); // Devuelve un array con los GameObjects
                    for (int i = 0; i < players.Length; i++)
                    {
                        var player = players[i];
                        int cantidad = Random.Range(10, 100);
                        // 5º El servidor hace el cambio en la NetworkVariable
                        player.QuitarVida(cantidad);
                    }
                }
                if (GUILayout.Button("Cambiar el color"))
                {
                    var players1 = GameObject.FindObjectsOfType<Player>(); // Devuelve un array con los componentes
                    var playersGameObjects1 = GameObject.FindGameObjectsWithTag("Player"); // Devuelve un array con los GameObjects
                    for (int i = 0; i < players1.Length; i++)
                    {
                        var player = players1[i];
                        Color color = new Color(Random.value, Random.value, Random.value);
                        player.CambiarColor(color);
                    }
                }
            }
            // Si soy el cliente, mostrar boton de cambio de color al servidor
        }

        GUILayout.EndArea();
    }

    // Cada boton inicia un tipo de servicio en el ordenador
    static void StartButtons()
    {
        if (GUILayout.Button("Host")) NetworkManager.Singleton.StartHost();
        if (GUILayout.Button("Client")) NetworkManager.Singleton.StartClient();
        if (GUILayout.Button("Server")) NetworkManager.Singleton.StartServer();
    }

    // Muestra el estado del servicio multijugador
    static void StatusLabels()
    {
        var mode = NetworkManager.Singleton.IsHost ?
            "Host" : NetworkManager.Singleton.IsServer ? "Server" : "Client";

        GUILayout.Label("Transport: " +
            NetworkManager.Singleton.NetworkConfig.NetworkTransport.GetType().Name);
        GUILayout.Label("Mode: " + mode);
    }

    // Peticion de movimiento para el jugador
    static void SubmitNewPosition()
    {
        if (GUILayout.Button(NetworkManager.Singleton.IsServer ? "Move" : "Request Position Change"))
        {
            var playerObject = NetworkManager.Singleton.SpawnManager.GetLocalPlayerObject();
            var player = playerObject.GetComponent<Player>();
            player.Move();
        }
    }
}
