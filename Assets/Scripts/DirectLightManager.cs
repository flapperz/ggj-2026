using UnityEngine;

public class LightController : MonoBehaviour
{
    private Light downlight;

    void Awake()
    {
        // Find the GameObject named "downlight"
        GameObject lightObj = gameObject;

        if (lightObj != null)
        {
            downlight = lightObj.GetComponent<Light>();
        }

        if (downlight == null)
        {
            Debug.LogError("LightController: Could not find Light component on 'downlight'!");
        }
    }

    void Start()
    {
        GameManager.Instance.OnPolarityChanged += HandlePolarityChange;
        HandlePolarityChange(GameManager.Instance.CurrentPolarity);
    }

    /// <summary>
    /// Sets the light color based on an index: 0=Orange, 1=Green, 2=Yellow-ish
    /// </summary>
    public void HandlePolarityChange(Polarity newPolarity)
    {
        if (downlight == null) return;

        string hexColor = newPolarity switch
        {
            Polarity.Angry => "#FF6219", // Orange
            Polarity.Happy => "#36C073", // Green
            Polarity.Neutral => "#9C9C56", // Neutral/Yellow
            _ => "#FFFFFF"  // Default White
        };

        if (ColorUtility.TryParseHtmlString(hexColor, out Color newColor))
        {
            downlight.color = newColor;

            // If you are using Realtime Global Illumination, 
            // you might also want to update the intensity or bounce
            // downlight.intensity = 1.0f; 
        }
    }
}