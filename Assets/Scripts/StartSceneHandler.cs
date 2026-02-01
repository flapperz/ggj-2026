using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.InputSystem;

public class StartSceneHandler : MonoBehaviour
{
    private bool loading = false;
    private InputAction triggerAction;

    void Start()
    {
        var interactable = GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRSimpleInteractable>();
        if (interactable != null)
            interactable.selectEntered.AddListener(OnXRSelect);

        // Listen for any XR controller trigger/grip/primary button
        triggerAction = new InputAction("AnyVRButton", InputActionType.Button);
        triggerAction.AddBinding("<XRController>{LeftHand}/triggerPressed");
        triggerAction.AddBinding("<XRController>{RightHand}/triggerPressed");
        triggerAction.AddBinding("<XRController>{LeftHand}/gripPressed");
        triggerAction.AddBinding("<XRController>{RightHand}/gripPressed");
        triggerAction.AddBinding("<XRController>{LeftHand}/primaryButton");
        triggerAction.AddBinding("<XRController>{RightHand}/primaryButton");
        triggerAction.performed += _ => LoadGame();
        triggerAction.Enable();
    }

    void OnDestroy()
    {
        if (triggerAction != null)
        {
            triggerAction.Disable();
            triggerAction.Dispose();
        }
    }

    void Update()
    {
        if (loading) return;

        // Fallback: any key or mouse click
        if (Input.anyKeyDown || Input.GetMouseButtonDown(0))
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

    public void OnXRSelect(SelectEnterEventArgs args)
    {
        LoadGame();
    }
}
