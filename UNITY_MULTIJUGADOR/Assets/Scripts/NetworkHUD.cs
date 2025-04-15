using UnityEngine;
using Unity.Netcode;


/// <summary>
/// <para>Basic UI for starting/joining games.</para>
/// Shows connection status and lets players host/join games.
/// </summary>
public class NetworkHUD : MonoBehaviour
{
    void OnGUI()
    {
        // Creamos una interfaz en pantalla
        GUILayout.BeginArea(new Rect(10, 10, 300, 300));
        // Si no somos un cliente o un servidor
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
            //SubmitNewPosition();
        }

        GUILayout.EndArea();
    }

    // Cada boton inicia un tipo de servicio en el ordenador
    /// <summary>
    /// Draw buttons for hosting/joining games
    /// </summary>
    static void StartButtons()
    {
        if (GUILayout.Button("Host")) NetworkManager.Singleton.StartHost();
        if (GUILayout.Button("Client")) NetworkManager.Singleton.StartClient();
        if (GUILayout.Button("Server")) NetworkManager.Singleton.StartServer();
    }

    // Muestra el estado del servicio multijugador
    /// <summary>
    /// Show current connection status
    /// </summary>
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
