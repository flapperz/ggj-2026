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
        Material targetMat = GameManager.Instance.GetMaterial(objectPolarity);
        myRenderer.material = targetMat;
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
        // UpdateVisuals(shouldEnable);
    }

    // private void UpdateVisuals(bool isSolid)
    // {
    //     if (myRenderer == null) return;

    //     // Example: Change opacity or switch material based on state
    //     // This uses the material from the GameManager to match the look
    //     Material targetMat = GameManager.Instance.GetMaterial(objectPolarity);
    //     myRenderer.material = targetMat;

    //     // If it's not solid (disabled), maybe make it look "ghostly" or disable renderer
    //     // myRenderer.enabled = isSolid; 
    // }
}