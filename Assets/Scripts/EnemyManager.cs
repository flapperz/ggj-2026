using UnityEngine;

public class FlyingEnemy : MonoBehaviour
{
    [Header("Targeting")]
    public string playerTag = "Player";
    public float detectionRange = 10.0f;

    [Header("Movement")]
    public float speed = 3.0f;

    private Transform playerTransform;

    void Start()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag(playerTag);
        if (playerObj != null)
        {
            playerTransform = playerObj.transform;
        }
    }

    void Update()
    {
        if (playerTransform == null) return;

        float distanceToPlayer = Vector2.Distance(transform.position, playerTransform.position);

        if (distanceToPlayer <= detectionRange)
        {
            ChasePlayer();
        }

    }

    private void ChasePlayer()
    {
        Vector3 targetPosition = new Vector3(
            playerTransform.position.x, 
            playerTransform.position.y, 
            transform.position.z 
        );

        transform.position = Vector3.MoveTowards(
            transform.position, 
            targetPosition, 
            speed * Time.deltaTime
        );

        // Face Direction
        if (playerTransform.position.x > transform.position.x)
            transform.localScale = new Vector3(1, 1, 1);
        else
            transform.localScale = new Vector3(-1, 1, 1);
    }

    // --- HIT LOGIC ---
    private void OnTriggerEnter(Collider other)
    {
        // 1. Check if the object we hit is the Player
        if (other.CompareTag(playerTag))
        {
            // 2. Try to get the Player script
            Player playerScript = other.GetComponent<Player>();

            if (playerScript != null)
            {
                // 3. Call the dummy function
                playerScript.OnHit();
            }

            // 4. Delete self
            Destroy(gameObject);
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);
    }
}