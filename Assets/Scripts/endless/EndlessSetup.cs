using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;

/// <summary>
/// Loads Level 1 additively into a StageRoot/WorldMover hierarchy
/// and wires VR controllers.
/// Scene is in cm (1 unit = 1cm). Level 1 content is scaled by
/// levelScale so it fills the stage (level ~20 units wide → 400cm).
/// Level (0,0,0) maps to StageRoot center.
///
/// Required scene setup:
///   - A GameObject named "StageRoot" (position where the diorama sits)
///     - A child named "WorldMover" (will hold Level 1 content + StageWorldMover)
///   If these don't exist, the script creates them at default positions.
/// </summary>
public class EndlessSetup : MonoBehaviour
{
    [Header("Stage Root")]
    [Tooltip("Drag the StageRoot from the scene. If empty, looks for 'StageRoot' by name or creates one.")]
    public Transform stageRoot;

    [Header("Stage Layout (cm)")]
    [Tooltip("Where the player appears in stage-local X. 0 = center.")]
    public float stageCenterX = 0f;

    [Tooltip("Y offset to align level ground with stage floor.")]
    public float stageBaseY = -112.5f;

    Player playerRef;

    void Start()
    {
        Build();
    }

    void Build()
    {
        // ================================================================
        // 1. FIND OR CREATE STAGE ROOT
        // ================================================================
        if (stageRoot == null)
        {
            GameObject found = GameObject.Find("StageRoot");
            if (found != null)
            {
                stageRoot = found.transform;
                Debug.Log("[HologramSetup] Found StageRoot in scene");
            }
            else
            {
                GameObject stageObj = new GameObject("StageRoot");
                stageRoot = stageObj.transform;
                stageRoot.position = new Vector3(0f, 150f, 300f);
                Debug.Log("[HologramSetup] Created StageRoot at default position (0, 150, 300)");
            }
        }

    }

    IEnumerator WaitForRightController(HologramShooter shooter)
    {
        float timeout = 15f;
        float elapsed = 0f;

        while (elapsed < timeout)
        {
            Transform rightCtrl = FindRightControllerTransform();
            if (rightCtrl != null)
            {
                GameObject xrOrigin = GameObject.Find("XR Origin (XR Rig)");
                if (xrOrigin != null)
                {
                    var modalityManager = xrOrigin.GetComponent<UnityEngine.XR.Interaction.Toolkit.Inputs.XRInputModalityManager>();
                    if (modalityManager != null)
                    {
                        modalityManager.enabled = false;
                        Debug.Log("[HologramSetup] Disabled XRInputModalityManager");
                    }
                }

                rightCtrl.gameObject.SetActive(true);
                DisableRightHandInteractors(rightCtrl.gameObject);
                shooter.vrRayOrigin = rightCtrl;
                Debug.Log($"[HologramSetup] RIGHT CONTROLLER FOUND after {elapsed:F1}s — shooting enabled!");
                yield break;
            }
            elapsed += 0.5f;
            yield return new WaitForSeconds(0.5f);
        }

        Debug.LogWarning("[HologramSetup] Right Controller never appeared after 15s — mouse fallback only");
    }

    Transform FindRightControllerTransform()
    {
        GameObject go = GameObject.Find("Right Controller");
        if (go != null) return go.transform;

        GameObject xrOrigin = GameObject.Find("XR Origin (XR Rig)");
        if (xrOrigin != null)
        {
            Transform found = xrOrigin.transform.Find("Camera Offset/Right Controller");
            if (found != null) return found;
        }

        return null;
    }

    void DisableRightHandInteractors(GameObject rightController)
    {
        if (rightController == null) return;

        var selfBehaviours = rightController.GetComponents<MonoBehaviour>();
        foreach (var b in selfBehaviours)
        {
            string typeName = b.GetType().Name;
            if (typeName != "TrackedPoseDriver")
            {
                b.enabled = false;
            }
        }

        var interactors = rightController.GetComponentsInChildren<UnityEngine.XR.Interaction.Toolkit.Interactors.XRBaseInteractor>(true);
        foreach (var interactor in interactors)
            interactor.enabled = false;

        var lineRenderers = rightController.GetComponentsInChildren<LineRenderer>(true);
        foreach (var lr in lineRenderers)
            lr.enabled = false;

        var lineVisuals = rightController.GetComponentsInChildren<UnityEngine.XR.Interaction.Toolkit.Interactors.Visuals.XRInteractorLineVisual>(true);
        foreach (var lv in lineVisuals)
            lv.enabled = false;
    }
}
