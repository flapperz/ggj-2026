using UnityEngine;

/// <summary>
/// Follows the target (Player) for a 2.5D side-view camera.
/// Maintains a fixed offset from the player, smoothed.
/// </summary>
public class CameraFollow : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private Vector3 offset = new Vector3(0f, 200f, -1000f);
    [SerializeField] private float smoothSpeed = 5f;

    private void LateUpdate()
    {
        if (target == null) return;

        Vector3 desiredPosition = target.position + offset;
        // Lock Z to offset value (2.5D - no Z tracking)
        desiredPosition.z = offset.z;

        Vector3 smoothed = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed * Time.deltaTime);
        transform.position = smoothed;
    }
}
