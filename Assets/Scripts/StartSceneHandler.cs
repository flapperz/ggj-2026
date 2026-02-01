using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.XR.Interaction.Toolkit;

public class StartSceneHandler : MonoBehaviour
{
    private bool loading = false;

    void Start()
    {
        var interactable = GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRSimpleInteractable>();
        if (interactable != null)
            interactable.selectEntered.AddListener(OnXRSelect);
    }

    void Update()
    {
        if (loading) return;

        // Any key or mouse click or touch loads the game
        if (Input.anyKeyDown || Input.GetMouseButtonDown(0) || Input.touchCount > 0)
        {
            LoadGame();
        }
    }

    public void LoadGame()
    {
        if (loading) return;
        loading = true;
        SceneManager.LoadScene("EnlessScene");
    }

    // Called by XR interactable
    public void OnXRSelect(SelectEnterEventArgs args)
    {
        LoadGame();
    }
}
