using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Unity.Netcode;

public class MenuCameraManager : MonoBehaviour
{
    public Camera[] cameras;
    public float switchInterval = 5f;

    private int currentIndex = 0;

    [Header("Post-Processing")]
    public Volume volume;
    private ChromaticAberration chroma;
    private LensDistortion distortion;

    private void Start()
    {
        // If the NetworkManager exists AND we're already connected (as a client only)
        if (NetworkManager.Singleton != null &&
            NetworkManager.Singleton.IsClient &&
            !NetworkManager.Singleton.IsServer)
        {
            enabled = false;
            return;
        }

        // Otherwise, allow the effect to run
        if (volume != null && volume.profile != null)
        {
            volume.profile.TryGet(out chroma);
            volume.profile.TryGet(out distortion);
        }

        SwitchToCamera(currentIndex);
        StartCoroutine(CycleCameras());
    }

    private IEnumerator CycleCameras()
    {
        while (true)
        {
            yield return new WaitForSeconds(switchInterval);
            currentIndex = (currentIndex + 1) % cameras.Length;
            StartCoroutine(GlitchEffect());
            SwitchToCamera(currentIndex);
        }
    }

    private void SwitchToCamera(int index)
    {
        for (int i = 0; i < cameras.Length; i++)
            cameras[i].enabled = (i == index);
    }

    private IEnumerator GlitchEffect()
    {
        float duration = 0.3f;
        float t = 0f;

        float defaultDistortion = 0.67f;

        while (t < duration)
        {
            t += Time.deltaTime;
            float strength = Mathf.Sin(t * 20f) * 0.4f + 0.5f; // for chromatic

            if (chroma != null)
                chroma.intensity.value = strength;

            if (distortion != null)
            {
                float wiggle = Mathf.Sin(t * 10f) * 0.2f; // ±0.2 variation
                distortion.intensity.value = defaultDistortion + wiggle;
            }

            yield return null;
        }

        if (chroma != null) chroma.intensity.value = 0.3f;
        if (distortion != null) distortion.intensity.value = defaultDistortion;
    }

}
