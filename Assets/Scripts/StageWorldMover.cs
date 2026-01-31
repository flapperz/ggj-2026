using UnityEngine;

/// <summary>
/// Keeps the player centered on stage by scrolling the world.
/// Attached to the WorldMover GameObject (parent of all Level 1 content).
/// Scene is in cm (1 unit = 1cm). Player localPosition is in level units,
/// so we multiply by levelScale to get cm offset.
/// </summary>
public class StageWorldMover : MonoBehaviour
{
    [Tooltip("The Level 1 Player transform to keep centered.")]
    public Transform playerTransform;

    [Tooltip("Where the player appears in stage-local X (cm). 0 = center.")]
    public float stageCenterX = 0f;

    [Tooltip("Y offset to align level ground with stage floor (cm).")]
    public float stageBaseY = -112.5f;

    [Tooltip("Level content scale factor (level units → cm).")]
    public float levelScale = 20f;

    void LateUpdate()
    {
        if (playerTransform == null) return;

        // Player localPosition is in level units, multiply by scale for cm
        float playerLocalX = playerTransform.localPosition.x;

        Vector3 pos = transform.localPosition;
        pos.x = stageCenterX - playerLocalX * levelScale;
        pos.y = stageBaseY;
        transform.localPosition = pos;
    }
}
