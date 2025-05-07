using TMPro;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Controls the visual behavior of floating damage text.
/// </summary>
public class DamageText : MonoBehaviour
{
    private TMP_Text tmp;
    private Color startColor = Color.red;
    private Color midColor = new Color(1f, 1f, 0f, 0.5f); // Yellow with half transparency
    private float duration = 1.5f; // Total lifespan
    private float timer;
    private Transform camTransform;

    void Awake()
    {
        tmp = GetComponent<TMP_Text>();
        tmp.color = startColor;
    }
    private void Start()
    {
        if (NetworkManager.Singleton.IsClient && NetworkManager.Singleton.IsConnectedClient)
        {
            var localPlayer = NetworkManager.Singleton.SpawnManager.GetLocalPlayerObject();
            if (localPlayer != null)
            {
                camTransform = localPlayer.GetComponent<PlayerAvatar>().playerCamera.transform;
            }
        }

        // fallback (scene camera or dev/testing)
        if (camTransform == null && Camera.main != null)
        {
            camTransform = Camera.main.transform;
        }
    }


    void Update()
    {
        timer += Time.deltaTime;

        // Rotate the text to face the camera (billboard)
        if (camTransform != null)
        {
            transform.rotation = Quaternion.LookRotation(transform.position - camTransform.position);
        }

        if (timer < 1f)
        {
            // First 1 second: Lerp from red to yellow with 0.5 alpha
            tmp.color = Color.Lerp(startColor, midColor, timer / 1f);
        }
        else if (timer < 1.5f)
        {
            // Next 0.5 seconds: Fade out
            float t = (timer - 1f) / 0.5f;
            tmp.color = Color.Lerp(midColor, new Color(midColor.r, midColor.g, midColor.b, 0), t);
        }
        else
        {
            Destroy(gameObject);
        }

        // Optionally float upward
        transform.position += Vector3.up * Time.deltaTime * 0.5f;
    }

    /// <summary>
    /// Initializes the text with damage value and random offset.
    /// </summary>
    public void Initialize(float amount)
    {
        tmp.text = Mathf.RoundToInt(amount).ToString();
        transform.position += new Vector3(Random.Range(-0.2f, 0.2f), Random.Range(0.5f, 1f), 0);
    }
}
