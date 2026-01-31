using UnityEngine;
using UnityEngine.InputSystem; // Required for the New Input System
using UnityEngine;

public class SnapCamera : MonoBehaviour
{
    public Transform player;
    public float dampingTime = 0.15f; // smaller = tighter, larger = floatier

    private Vector3 offset;
    private Vector3 velocity; // required by SmoothDamp

    void Start()
    {
        // Capture initial scene offset
        offset = transform.position - player.position;
    }

    void LateUpdate()
    {
        Vector3 targetPos = player.position + offset;
        transform.position = Vector3.SmoothDamp(
            transform.position,
            targetPos,
            ref velocity,
            dampingTime
        );
    }
}
