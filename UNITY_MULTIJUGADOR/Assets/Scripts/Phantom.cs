using UnityEngine;
using System.Collections;

public class Phantom : MonoBehaviour
{
    private Material phantomMat;
    private float fadeTime = 1f;
    private float lifeDuration = 3f;

    public void Init(float maxVisibleTime, Transform playerTransform)
    {
        phantomMat = GetComponentInChildren<Renderer>().material;
        lifeDuration = maxVisibleTime;

        float dist = Vector3.Distance(transform.position, playerTransform.position);
        float fadeDelay = Mathf.Clamp(lifeDuration - dist, 0.5f, lifeDuration);

        StartCoroutine(FadeOutAfterDelay(fadeDelay));
    }

    private IEnumerator FadeOutAfterDelay(float delay)
    {
        yield return new WaitForSecondsRealtime(delay); // Use real time

        float t = 0f;
        while (t < fadeTime)
        {
            t += Time.unscaledDeltaTime; // Unscaled to ignore Time.timeScale issues
            float alpha = Mathf.Lerp(1f, 0f, t / fadeTime);
            if (phantomMat != null)
                phantomMat.SetFloat("_Alpha", alpha);
            yield return null;
        }

        Destroy(gameObject);
    }
}
