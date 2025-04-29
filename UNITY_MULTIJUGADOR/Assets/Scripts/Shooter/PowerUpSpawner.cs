using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;

public class PowerUpSpawner : NetworkBehaviour
{
    [Tooltip("Lista de prefabs de power-ups")]
    public List<GameObject> powerUpPrefabs;

    [Tooltip("Tiempo entre spawn de power-ups")]
    public float spawnInterval = 15f;

    private float nextSpawnTime = 0f;
    private List<Transform> spawnPoints = new List<Transform>();

    public override void OnNetworkSpawn()
    {
        if (!IsServer)
        {
            return;
        }
        // Buscar todos los puntos con el tag "PowerUpSpawn"
        GameObject[] points = GameObject.FindGameObjectsWithTag("PowerUpSpawn");
        foreach (GameObject point in points)
        {
            spawnPoints.Add(point.transform);
        }

        nextSpawnTime = Time.time + spawnInterval;
    }

    private void Update()
    {
        if (!IsServer || spawnPoints.Count == 0 || powerUpPrefabs.Count == 0) return;

        if (Time.time >= nextSpawnTime)
        {
            SpawnRandomPowerUp();
            nextSpawnTime = Time.time + spawnInterval;
        }
    }

    private void SpawnRandomPowerUp()
    {
        int randomIndex = Random.Range(0, powerUpPrefabs.Count);
        int spawnIndex = Random.Range(0, spawnPoints.Count);

        GameObject powerUp = Instantiate(powerUpPrefabs[randomIndex], spawnPoints[spawnIndex].position, Quaternion.identity);
        powerUp.GetComponent<NetworkObject>().Spawn();
    }
}