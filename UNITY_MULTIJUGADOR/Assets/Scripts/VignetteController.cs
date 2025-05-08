using UnityEngine.UI;
using UnityEngine;
using System.Collections;

public class VignetteController : MonoBehaviour
{
    public RawImage vignetteImage; // Assign in Inspector
    private Material vignetteMat;

    private void Awake()
    {
        vignetteMat = Instantiate(vignetteImage.material);
        vignetteImage.material = vignetteMat;
        vignetteImage.gameObject.SetActive(true);
    }

    public void TriggerVignette(Color color, float duration, float coverage = 0.6f)
    {
        StartCoroutine(VignettePulse(color, duration, coverage));
    }

    private IEnumerator VignettePulse(Color color, float duration, float coverage)
    {
        vignetteMat.SetColor("_VignetteColor", color);
        vignetteMat.SetFloat("_Coverage", coverage);

        float t = 0f;
        while (t < duration)
        {
            float strength = Mathf.Lerp(1f, 0f, t / duration);
            vignetteMat.SetFloat("_Intensity", strength);
            t += Time.deltaTime;
            yield return null;
        }

        vignetteMat.SetFloat("_Intensity", 0f);
    }
}