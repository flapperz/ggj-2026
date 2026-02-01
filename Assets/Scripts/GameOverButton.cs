using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.XR.Interaction.Toolkit;

[RequireComponent(typeof(Collider))]
public class GameOverButton : MonoBehaviour
{
    public enum ButtonAction { TryAgain, Exit }
    public ButtonAction action;

    void Start()
    {
        // Ensure there's an XRSimpleInteractable for VR pointer clicks
        if (GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRSimpleInteractable>() == null)
        {
            var interactable = gameObject.AddComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRSimpleInteractable>();
            interactable.selectEntered.AddListener(OnXRSelect);
        }
        else
        {
            GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRSimpleInteractable>().selectEntered.AddListener(OnXRSelect);
        }
    }

    void OnXRSelect(SelectEnterEventArgs args)
    {
        Execute();
    }

    // Fallback for mouse/desktop
    void OnMouseDown()
    {
        Execute();
    }

    void Execute()
    {
        if (action == ButtonAction.TryAgain)
        {
            SceneManager.LoadScene("EnlessScene");
        }
        else if (action == ButtonAction.Exit)
        {
            Application.Quit();
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#endif
        }
    }
}
