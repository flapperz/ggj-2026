using UnityEngine;
using TMPro; // Required for TextMeshPro

public class ScoreTextUpdate : MonoBehaviour
{
    private TMP_Text scoreText;

    void Awake()
    {
        // Automatically get the component on this object
        scoreText = GetComponent<TMP_Text>();
    }

    void Update()
    {
        if (GameManager.Instance != null && scoreText != null)
        {
            // Use string interpolation to format the score
            scoreText.text = $"{(int)GameManager.Instance.Score}";
        }
    }
}