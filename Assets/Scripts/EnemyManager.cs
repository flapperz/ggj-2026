using UnityEngine;

public class FlyingEnemy : MonoBehaviour
{
    [Header("Targeting")]
    public string playerTag = "Player";
    
    [Header("Movement")]
    public float speed = 1.0f;
    public float dashSpeed = 5.0f; 
    public float lockRadius = 15.0f;
    public float pauseDuration = 0.5f; // Time to stay still when locking

    private Transform playerTransform;
    private Vector3 lockedDirection;
    
    // State Tracking
    private bool isDirectionLocked = false;
    private bool isPaused = false;
    private float pauseTimer = 0f;

    void Start()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag(playerTag);
        if (playerObj != null) playerTransform = playerObj.transform;
    }

    void Update()
    {
        // 1. Check Bounds (Delete if x, y, or z > 40)
        if (Mathf.Abs(transform.position.x) > 40f || 
            Mathf.Abs(transform.position.y) > 40f || 
            Mathf.Abs(transform.position.z) > 40f)
        {
            Destroy(gameObject);
            return;
        }

        if (playerTransform == null) return;

        HandleMovement();
    }

    private void HandleMovement()
    {
        float distanceToPlayer = Vector3.Distance(transform.position, playerTransform.position);

        // STATE 1: Initial Follow
        if (!isDirectionLocked)
        {
            transform.position = Vector3.MoveTowards(
                transform.position, 
                playerTransform.position, 
                speed * Time.deltaTime
            );

            if (distanceToPlayer <= lockRadius)
            {
                // Start the locking sequence
                lockedDirection = (playerTransform.position - transform.position).normalized;
                isDirectionLocked = true;
                isPaused = true; 
                pauseTimer = pauseDuration;
                Debug.Log("[Enemy] Locking On... Pausing.");
            }
            
            FaceDirection(playerTransform.position.x - transform.position.x);
        }
        // STATE 2: The Telegraph Pause
        else if (isPaused)
        {
            pauseTimer -= Time.deltaTime;
            if (pauseTimer <= 0f)
            {
                isPaused = false;
                Debug.Log("[Enemy] Dashing!");
            }
        }
        // STATE 3: The Dash
        else
        {
            transform.position += lockedDirection * dashSpeed * Time.deltaTime;
        }
    }

    private void FaceDirection(float horizontalDelta)
    {
        if (horizontalDelta > 0.1f)
            transform.localScale = new Vector3(1, 1, 1);
        else if (horizontalDelta < -0.1f)
            transform.localScale = new Vector3(-1, 1, 1);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(playerTag))
        {
            Player playerScript = other.GetComponent<Player>();
            if (playerScript != null) playerScript.OnHit();
            Destroy(gameObject);
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = isDirectionLocked ? Color.red : Color.cyan;
        Gizmos.DrawWireSphere(transform.position, lockRadius);
    }
}