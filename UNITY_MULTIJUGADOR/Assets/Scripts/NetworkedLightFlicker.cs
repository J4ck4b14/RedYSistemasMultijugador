using UnityEngine;
using Unity.Netcode;
using System.Collections;

[RequireComponent(typeof(Renderer))]
public class NetworkedLightFlicker : NetworkBehaviour
{
    [Header("Emission Settings")]
    [Tooltip("Base color for the emissive material and spotlight.")]
    public Color emissiveColor = Color.white;
    [Tooltip("Multiplier for how bright the emission and spotlight get.")]
    [Range(0f, 10f)] public float emissionIntensity = 1f;

    [Header("Flicker Timing")]
    [Tooltip("Seconds to wait between flicker sequences.")]
    public float minInterval = 5f;
    public float maxInterval = 15f;
    [Tooltip("How many quick flickers per sequence.")]
    public int minFlickers = 3;
    public int maxFlickers = 6;
    [Tooltip("Duration of each OFF/ON phase within a flicker.")]
    public float flickerSpeedMin = 0.05f;
    public float flickerSpeedMax = 0.2f;

    [Header("Optional Effects")]
    [Tooltip("Child Spotlight; if left blank, will search children for one.")]
    public Light spotLight;
    [Tooltip("Particle system to play at each ON phase (e.g. sparks).")]
    public ParticleSystem flickerParticles;

    // **internals**
    private Renderer _renderer;
    private MaterialPropertyBlock _propBlock;

    void Awake()
    {
        _renderer = GetComponent<Renderer>();
        _propBlock = new MaterialPropertyBlock();

        if (spotLight == null)
            spotLight = GetComponentInChildren<Light>();
    }

    public override void OnNetworkSpawn()
    {
        // Sync initial “ON” state on all clients
        ApplyEmission(emissionIntensity);

        // Only the server drives the flicker timing
        if (IsServer)
            StartCoroutine(ServerFlickerLoop());
    }

    IEnumerator ServerFlickerLoop()
    {
        while (true)
        {
            // wait a random interval before next burst
            yield return new WaitForSeconds(Random.Range(minInterval, maxInterval));

            int count = Random.Range(minFlickers, maxFlickers + 1);
            for (int i = 0; i < count; i++)
            {
                // broadcast OFF
                FlickerClientRpc(false);
                yield return new WaitForSeconds(Random.Range(flickerSpeedMin, flickerSpeedMax));

                // broadcast ON
                FlickerClientRpc(true);
                yield return new WaitForSeconds(Random.Range(flickerSpeedMin, flickerSpeedMax));
            }
        }
    }

    // This RPC runs on every client (including host)
    [ClientRpc]
    void FlickerClientRpc(bool on)
    {
        float intensity = on ? emissionIntensity : 0f;
        ApplyEmission(intensity);

        if (on && flickerParticles != null)
            flickerParticles.Play();
    }

    // Local helper to push emission into MPB + spotlight
    void ApplyEmission(float intensity)
    {
        Color finalCol = emissiveColor * intensity;
        _renderer.GetPropertyBlock(_propBlock);
        _propBlock.SetColor("_EmissionColor", finalCol);
        _renderer.SetPropertyBlock(_propBlock);

        if (spotLight != null)
        {
            spotLight.color = emissiveColor;
            spotLight.intensity = intensity;
        }
    }

    // Live-update in Editor
    void OnValidate()
    {
        if (_renderer != null && _propBlock != null)
            ApplyEmission(emissionIntensity);
    }
}
