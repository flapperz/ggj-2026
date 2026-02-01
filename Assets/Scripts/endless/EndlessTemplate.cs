using UnityEngine;
public class EndlessTemplate : MonoBehaviour
{
public Color gizmoColor = Color.green;

    private void OnDrawGizmos()
    {
        // Define the dimensions
        Vector3 size = new Vector3(50f, 25f, 5f);

        // Calculate the offset:
        // Left: Origin is at min X, so move center by half width
        // Bottom: Origin is at min Y, so move center by half height
        // Middle: Origin is at center Z, so offset is 0
        Vector3 offset = new Vector3(size.x / 2, size.y / 2, 0);

        // Apply the transform's position, rotation, and scale
        Gizmos.matrix = transform.localToWorldMatrix;

        Gizmos.color = gizmoColor;
        
        // Draw the wireframe cube
        Gizmos.DrawWireCube(offset, size);

        // Optional: Draw a small sphere at the origin (0,0,0) to verify
        Gizmos.color = Color.red;
        Gizmos.DrawSphere(Vector3.zero, 1f);
    }
}