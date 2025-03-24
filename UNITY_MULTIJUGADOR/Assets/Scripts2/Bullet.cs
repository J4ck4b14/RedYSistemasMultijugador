using UnityEngine;
using Unity.Netcode;

public class Bullet : NetworkBehaviour
{
    private bool destroyed = false;

    private void OnCollisionEnter(Collision collision)
    {
        if (destroyed)
            return;

        destroyed = true;

        // Cuando el servidor detecta una colision de bala con un jugador
    }
}
