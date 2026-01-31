using UnityEngine;
using System; // Required for 'Action'

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    // 1. Changed to Property so it cannot be modified directly from outside
    public Polarity CurrentPolarity { get; private set; } = Polarity.Neutral;

    // 2. The Event (Action is a standard C# delegate that takes one parameter)
    public event Action<Polarity> OnPolarityChanged;

    [Header("Materials")]
    public Material happyMaterial;
    public Material angryMaterial;
    public Material neutralMaterial;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    // 3. The Function to set polarity and emit the event
    public void SetPolarity(Polarity newPolarity)
    {
        // Optimization: Don't fire event if the value hasn't actually changed
        if (CurrentPolarity == newPolarity) return;

        CurrentPolarity = newPolarity;

        // Emit the event to all subscribers
        // The '?' checks if there are any subscribers before calling Invoke
        OnPolarityChanged?.Invoke(CurrentPolarity);
    }

    public Material GetMaterial(Polarity polarity)
    {
        switch (polarity)
        {
            case Polarity.Happy: return happyMaterial;
            case Polarity.Angry: return angryMaterial;
            default: return neutralMaterial;
        }
    }
}