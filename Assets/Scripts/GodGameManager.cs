using UnityEngine;

/// <summary>
/// Core manager for the VR God Game.
///
/// Architecture:
/// - The VR player stands in 3D space looking at a screen (Quad).
/// - A separate orthographic Camera renders a 2D game world into a RenderTexture.
/// - That RenderTexture is displayed on the Quad, creating a "TV screen" effect.
/// - The VR player can shoot (raycast) at the screen to interact with the 2D world.
/// </summary>
public class GodGameManager : MonoBehaviour
{
    public static GodGameManager Instance { get; private set; }

    [Header("Screen Setup")]
    [Tooltip("The Quad in VR space that displays the 2D game")]
    public Transform gameScreen;

    [Tooltip("Orthographic camera that renders the 2D world")]
    public Camera gameCamera2D;

    [Header("Runtime")]
    public RenderTexture screenRT;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    void Start()
    {
        SetupRenderTexture();
    }

    void SetupRenderTexture()
    {
        // Create RT if not assigned
        if (screenRT == null)
        {
            screenRT = new RenderTexture(1920, 1080, 16);
            screenRT.name = "GodGameScreenRT";
            screenRT.filterMode = FilterMode.Bilinear;
            screenRT.Create();
        }

        // Assign RT to the 2D camera
        if (gameCamera2D != null)
        {
            gameCamera2D.targetTexture = screenRT;
        }
        else
        {
            Debug.LogWarning("[GodGameManager] No 2D camera assigned!");
        }

        // Display RT on the screen quad
        if (gameScreen != null)
        {
            Renderer rend = gameScreen.GetComponent<Renderer>();
            if (rend != null)
            {
                // Try URP Unlit first, fall back to built-in
                Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
                if (shader == null) shader = Shader.Find("Unlit/Texture");
                if (shader == null) shader = Shader.Find("Unlit/Color");

                Material mat = new Material(shader);
                mat.mainTexture = screenRT;
                // URP Unlit uses _BaseMap, built-in uses _MainTex
                if (mat.HasProperty("_BaseMap"))
                    mat.SetTexture("_BaseMap", screenRT);
                rend.material = mat;
                Debug.Log($"[GodGameManager] Screen material shader: {shader.name}");
            }
        }
        else
        {
            Debug.LogWarning("[GodGameManager] No game screen quad assigned!");
        }
    }

    /// <summary>
    /// Converts a 3D hit point on the screen quad into 2D game-world coordinates.
    /// The quad's local X/Y range is -0.5 to 0.5 (Unity default Quad).
    /// </summary>
    public Vector2 ScreenHitTo2DWorld(Vector3 vrHitPoint)
    {
        if (gameScreen == null || gameCamera2D == null)
            return Vector2.zero;

        // Convert VR hit point into screen quad's local space
        Vector3 local = gameScreen.InverseTransformPoint(vrHitPoint);

        // Quad local coords: X in [-0.5, 0.5], Y in [-0.5, 0.5]
        float viewportX = local.x + 0.5f;
        float viewportY = local.y + 0.5f;

        // Clamp to valid viewport
        viewportX = Mathf.Clamp01(viewportX);
        viewportY = Mathf.Clamp01(viewportY);

        // Convert viewport to 2D world pos using the ortho camera
        Vector3 worldPos = gameCamera2D.ViewportToWorldPoint(new Vector3(viewportX, viewportY, 0f));
        return new Vector2(worldPos.x, worldPos.y);
    }
}
