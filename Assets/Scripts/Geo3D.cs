using UnityEngine;

/// <summary>
/// Factory for 3D prototype geometry used in the VR environment.
/// Creates colored primitives with optional physics for the VR space around the screen.
/// Produces: Cube, Sphere, Cylinder, Capsule, Ramp/Wedge, Wall, Pillar, Frame.
/// </summary>
public static class Geo3D
{
    static Material _matCache;

    static Material GetColorMat(Color color)
    {
        // Try multiple shaders in order of preference
        // URP shaders get stripped at runtime if not referenced by a material asset,
        // so we fall back to built-in shaders that are always available.
        string[] shaderNames = new string[]
        {
            "Universal Render Pipeline/Lit",
            "Universal Render Pipeline/Simple Lit",
            "Standard",
            "Unlit/Color"
        };

        Shader shader = null;
        foreach (string name in shaderNames)
        {
            shader = Shader.Find(name);
            if (shader != null) break;
        }

        if (shader == null)
        {
            Debug.LogWarning("[Geo3D] No valid shader found, using fallback");
            shader = Shader.Find("Hidden/InternalErrorShader");
        }

        Material mat = new Material(shader);
        // URP uses _BaseColor, Standard uses _Color
        mat.SetColor("_BaseColor", color);
        mat.color = color;
        return mat;
    }

    static Material GetUnlitMat(Color color)
    {
        string[] shaderNames = new string[]
        {
            "Universal Render Pipeline/Unlit",
            "Unlit/Color"
        };

        Shader shader = null;
        foreach (string name in shaderNames)
        {
            shader = Shader.Find(name);
            if (shader != null) break;
        }

        if (shader == null)
            shader = Shader.Find("Unlit/Color");

        Material mat = new Material(shader);
        mat.SetColor("_BaseColor", color);
        mat.color = color;
        return mat;
    }

    // ─── PRIMITIVES ──────────────────────────────────────────

    public static GameObject Cube(Vector3 pos, Vector3 scale, Color color, Transform parent = null, bool physics = false)
    {
        return MakePrimitive("Cube", PrimitiveType.Cube, pos, scale, color, parent, physics);
    }

    public static GameObject Sphere(Vector3 pos, float radius, Color color, Transform parent = null, bool physics = false)
    {
        return MakePrimitive("Sphere", PrimitiveType.Sphere, pos, Vector3.one * radius * 2f, color, parent, physics);
    }

    public static GameObject Cylinder(Vector3 pos, float radius, float height, Color color, Transform parent = null, bool physics = false)
    {
        return MakePrimitive("Cylinder", PrimitiveType.Cylinder, pos, new Vector3(radius * 2f, height * 0.5f, radius * 2f), color, parent, physics);
    }

    public static GameObject Capsule(Vector3 pos, float radius, float height, Color color, Transform parent = null, bool physics = false)
    {
        return MakePrimitive("Capsule", PrimitiveType.Capsule, pos, new Vector3(radius * 2f, height * 0.5f, radius * 2f), color, parent, physics);
    }

    // ─── COMPOUND SHAPES ─────────────────────────────────────

    /// <summary>A wedge/ramp built from a custom mesh.</summary>
    public static GameObject Ramp(Vector3 pos, float width, float height, float depth, Color color, Transform parent = null)
    {
        GameObject go = new GameObject("Ramp");
        if (parent != null) go.transform.SetParent(parent);
        go.transform.localPosition = pos;

        MeshFilter mf = go.AddComponent<MeshFilter>();
        mf.mesh = CreateRampMesh(width, height, depth);

        MeshRenderer mr = go.AddComponent<MeshRenderer>();
        mr.material = GetColorMat(color);

        MeshCollider mc = go.AddComponent<MeshCollider>();
        mc.sharedMesh = mf.mesh;
        mc.convex = true;

        return go;
    }

    /// <summary>A flat wall panel.</summary>
    public static GameObject Wall(Vector3 pos, float width, float height, float thickness, Color color, Transform parent = null)
    {
        return Cube(pos, new Vector3(width, height, thickness), color, parent, false);
    }

    /// <summary>A tall pillar/column.</summary>
    public static GameObject Pillar(Vector3 pos, float radius, float height, Color color, Transform parent = null)
    {
        return Cylinder(pos, radius, height, color, parent, false);
    }

    /// <summary>A picture-frame border (4 cubes) - use for the VR game screen frame.</summary>
    public static GameObject Frame(Vector3 pos, float width, float height, float thickness, float depth, Color color, Transform parent = null)
    {
        GameObject frame = new GameObject("Frame");
        if (parent != null) frame.transform.SetParent(parent);
        frame.transform.localPosition = pos;

        float halfW = width * 0.5f;
        float halfH = height * 0.5f;

        // Top
        Cube(new Vector3(0, halfH, 0), new Vector3(width + thickness * 2, thickness, depth), color, frame.transform);
        // Bottom
        Cube(new Vector3(0, -halfH, 0), new Vector3(width + thickness * 2, thickness, depth), color, frame.transform);
        // Left
        Cube(new Vector3(-halfW - thickness * 0.5f, 0, 0), new Vector3(thickness, height, depth), color, frame.transform);
        // Right
        Cube(new Vector3(halfW + thickness * 0.5f, 0, 0), new Vector3(thickness, height, depth), color, frame.transform);

        return frame;
    }

    /// <summary>A simple table (slab + 4 legs).</summary>
    public static GameObject Table(Vector3 pos, float width, float depth, float height, Color topColor, Color legColor, Transform parent = null)
    {
        GameObject table = new GameObject("Table");
        if (parent != null) table.transform.SetParent(parent);
        table.transform.localPosition = pos;

        float slabH = 0.05f;
        float legR = 0.03f;

        // Top slab
        Cube(new Vector3(0, height, 0), new Vector3(width, slabH, depth), topColor, table.transform);

        // 4 Legs
        float lx = width * 0.4f;
        float lz = depth * 0.4f;
        Cylinder(new Vector3(-lx, height * 0.5f, -lz), legR, height, legColor, table.transform);
        Cylinder(new Vector3(lx, height * 0.5f, -lz), legR, height, legColor, table.transform);
        Cylinder(new Vector3(-lx, height * 0.5f, lz), legR, height, legColor, table.transform);
        Cylinder(new Vector3(lx, height * 0.5f, lz), legR, height, legColor, table.transform);

        return table;
    }

    // ─── INTERNAL HELPERS ────────────────────────────────────

    static GameObject MakePrimitive(string name, PrimitiveType type, Vector3 pos, Vector3 scale, Color color, Transform parent, bool physics)
    {
        GameObject go = GameObject.CreatePrimitive(type);
        go.name = name;
        if (parent != null) go.transform.SetParent(parent);
        go.transform.localPosition = pos;
        go.transform.localScale = scale;

        Renderer rend = go.GetComponent<Renderer>();
        rend.material = GetColorMat(color);

        if (physics)
        {
            Rigidbody rb = go.AddComponent<Rigidbody>();
            rb.mass = 1f;
        }

        return go;
    }

    static Mesh CreateRampMesh(float w, float h, float d)
    {
        Mesh mesh = new Mesh();
        mesh.name = "RampMesh";

        // 6 vertices for a wedge
        Vector3[] verts = new Vector3[]
        {
            // Bottom face (z=0)
            new Vector3(-w * 0.5f, 0, 0),         // 0: front-left-bottom
            new Vector3(w * 0.5f, 0, 0),           // 1: front-right-bottom
            // Bottom face (z=d)
            new Vector3(-w * 0.5f, 0, d),          // 2: back-left-bottom
            new Vector3(w * 0.5f, 0, d),           // 3: back-right-bottom
            // Top face (z=d, y=h)
            new Vector3(-w * 0.5f, h, d),          // 4: back-left-top
            new Vector3(w * 0.5f, h, d),           // 5: back-right-top
        };

        int[] tris = new int[]
        {
            // Bottom
            0, 2, 1, 1, 2, 3,
            // Back (vertical face)
            3, 2, 4, 3, 4, 5,
            // Slope (front face)
            0, 1, 5, 0, 5, 4,
            // Left side
            0, 4, 2,
            // Right side
            1, 3, 5,
        };

        mesh.vertices = verts;
        mesh.triangles = tris;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }
}
