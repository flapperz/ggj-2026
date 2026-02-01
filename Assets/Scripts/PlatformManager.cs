using UnityEngine;

[RequireComponent(typeof(Collider))] // Ensures a collider exists
public class PlatformManager : MonoBehaviour
{
    [Header("Settings")]
    public Polarity objectPolarity; // Assign this in the Inspector (e.g., Happy or Angry)

    private Collider myCollider;
    private Renderer myRenderer; // Optional: To visualize the change

    void Awake()
    {
        myCollider = GetComponent<Collider>();
        myRenderer = GetComponent<Renderer>();
    }

    void Start()
    {
        // 1. Subscribe to the event
        GameManager.Instance.OnPolarityChanged += HandlePolarityChange;

        // 2. Initialize state immediately (in case the game starts in a specific state)
        HandlePolarityChange(GameManager.Instance.CurrentPolarity);
        
        // Debug material
        // Material targetMat = GameManager.Instance.GetMaterial(objectPolarity);
        // myRenderer.material = targetMat;
    }

    void OnDestroy()
    {
        // 3. Unsubscribe to prevent memory leaks and errors
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnPolarityChanged -= HandlePolarityChange;
        }
    }

    // This method ONLY runs when the event is emitted
    private void HandlePolarityChange(Polarity globalPolarity)
    {
        if (objectPolarity == Polarity.Neutral)
        {
            return;
        }

        // Logic: Disable collider if Global Polarity matches this object's Polarity
        bool shouldEnable = (globalPolarity != objectPolarity);

        myCollider.enabled = shouldEnable;

        // Optional: Visual feedback (make it semi-transparent or change color)
        UpdateVisuals(!shouldEnable);
    }

    private void UpdateVisuals(bool isSolid)
    {
    if (myRenderer == null) return;

        // 1. Get the base material from the GameManager
        Material targetMat = GameManager.Instance.GetMaterial(objectPolarity);

        // 2. Assign the material to the renderer
        // Note: Accessing .material creates a unique instance clone so we don't mess up other objects
        myRenderer.material = targetMat;

        // 3. Get the current color of that material
        Color newColor = myRenderer.material.color;

        // 4. Set Alpha based on your rule:
        // isSolid (true) -> 0.5f (Semi-transparent)
        // isSolid (false) -> 1.0f (Fully Opaque)
        newColor.a = isSolid ? 0.5f : 1.0f;

        // 5. Apply the modified color back
        myRenderer.material.color = newColor;
    }
}