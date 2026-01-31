using UnityEngine;

/// <summary>
/// Factory for 2D prototype geometry sprites generated at runtime.
/// All shapes are white so SpriteRenderer.color can tint them.
/// Produces: Square, Circle, Triangle, Diamond, Capsule, Hexagon.
/// </summary>
public static class Geo2D
{
    const int TEX_SIZE = 64;

    // ─── SPRITES ─────────────────────────────────────────────

    public static Sprite Square()
    {
        Texture2D tex = new Texture2D(TEX_SIZE, TEX_SIZE);
        Color[] px = new Color[TEX_SIZE * TEX_SIZE];
        for (int i = 0; i < px.Length; i++) px[i] = Color.white;
        tex.SetPixels(px);
        tex.Apply();
        tex.filterMode = FilterMode.Point;
        return Sprite.Create(tex, new Rect(0, 0, TEX_SIZE, TEX_SIZE), new Vector2(0.5f, 0.5f), TEX_SIZE);
    }

    public static Sprite Circle()
    {
        Texture2D tex = new Texture2D(TEX_SIZE, TEX_SIZE);
        Color[] px = new Color[TEX_SIZE * TEX_SIZE];
        float r = TEX_SIZE * 0.5f;
        for (int y = 0; y < TEX_SIZE; y++)
        {
            for (int x = 0; x < TEX_SIZE; x++)
            {
                float dx = x - r + 0.5f;
                float dy = y - r + 0.5f;
                px[y * TEX_SIZE + x] = (dx * dx + dy * dy <= r * r) ? Color.white : Color.clear;
            }
        }
        tex.SetPixels(px);
        tex.Apply();
        tex.filterMode = FilterMode.Bilinear;
        return Sprite.Create(tex, new Rect(0, 0, TEX_SIZE, TEX_SIZE), new Vector2(0.5f, 0.5f), TEX_SIZE);
    }

    public static Sprite Triangle()
    {
        Texture2D tex = new Texture2D(TEX_SIZE, TEX_SIZE);
        Color[] px = new Color[TEX_SIZE * TEX_SIZE];
        for (int y = 0; y < TEX_SIZE; y++)
        {
            float ratio = (float)y / TEX_SIZE;
            float halfWidth = ratio * 0.5f * TEX_SIZE;
            float center = TEX_SIZE * 0.5f;
            for (int x = 0; x < TEX_SIZE; x++)
            {
                px[y * TEX_SIZE + x] = (x >= center - halfWidth && x <= center + halfWidth)
                    ? Color.white : Color.clear;
            }
        }
        tex.SetPixels(px);
        tex.Apply();
        tex.filterMode = FilterMode.Bilinear;
        return Sprite.Create(tex, new Rect(0, 0, TEX_SIZE, TEX_SIZE), new Vector2(0.5f, 0.5f), TEX_SIZE);
    }

    public static Sprite Diamond()
    {
        Texture2D tex = new Texture2D(TEX_SIZE, TEX_SIZE);
        Color[] px = new Color[TEX_SIZE * TEX_SIZE];
        float half = TEX_SIZE * 0.5f;
        for (int y = 0; y < TEX_SIZE; y++)
        {
            for (int x = 0; x < TEX_SIZE; x++)
            {
                float dx = Mathf.Abs(x - half + 0.5f);
                float dy = Mathf.Abs(y - half + 0.5f);
                px[y * TEX_SIZE + x] = (dx / half + dy / half <= 1f) ? Color.white : Color.clear;
            }
        }
        tex.SetPixels(px);
        tex.Apply();
        tex.filterMode = FilterMode.Bilinear;
        return Sprite.Create(tex, new Rect(0, 0, TEX_SIZE, TEX_SIZE), new Vector2(0.5f, 0.5f), TEX_SIZE);
    }

    public static Sprite Hexagon()
    {
        Texture2D tex = new Texture2D(TEX_SIZE, TEX_SIZE);
        Color[] px = new Color[TEX_SIZE * TEX_SIZE];
        float half = TEX_SIZE * 0.5f;
        // Hex uses: |x| <= r and |x|*0.5 + |y|*0.866 <= r
        float r = half * 0.95f;
        for (int y = 0; y < TEX_SIZE; y++)
        {
            for (int x = 0; x < TEX_SIZE; x++)
            {
                float ax = Mathf.Abs(x - half + 0.5f);
                float ay = Mathf.Abs(y - half + 0.5f);
                bool inside = ax <= r && (ax * 0.5f + ay * 0.866f) <= r;
                px[y * TEX_SIZE + x] = inside ? Color.white : Color.clear;
            }
        }
        tex.SetPixels(px);
        tex.Apply();
        tex.filterMode = FilterMode.Bilinear;
        return Sprite.Create(tex, new Rect(0, 0, TEX_SIZE, TEX_SIZE), new Vector2(0.5f, 0.5f), TEX_SIZE);
    }

    public static Sprite Star()
    {
        Texture2D tex = new Texture2D(TEX_SIZE, TEX_SIZE);
        Color[] px = new Color[TEX_SIZE * TEX_SIZE];
        float half = TEX_SIZE * 0.5f;
        int points = 5;
        float outerR = half * 0.95f;
        float innerR = half * 0.4f;

        // Precompute star polygon vertices
        Vector2[] verts = new Vector2[points * 2];
        for (int i = 0; i < points * 2; i++)
        {
            float angle = Mathf.PI * 0.5f + i * Mathf.PI / points;
            float radius = (i % 2 == 0) ? outerR : innerR;
            verts[i] = new Vector2(
                half + Mathf.Cos(angle) * radius,
                half + Mathf.Sin(angle) * radius
            );
        }

        for (int y = 0; y < TEX_SIZE; y++)
        {
            for (int x = 0; x < TEX_SIZE; x++)
            {
                px[y * TEX_SIZE + x] = PointInPolygon(x, y, verts) ? Color.white : Color.clear;
            }
        }
        tex.SetPixels(px);
        tex.Apply();
        tex.filterMode = FilterMode.Bilinear;
        return Sprite.Create(tex, new Rect(0, 0, TEX_SIZE, TEX_SIZE), new Vector2(0.5f, 0.5f), TEX_SIZE);
    }

    static bool PointInPolygon(float px, float py, Vector2[] verts)
    {
        bool inside = false;
        int j = verts.Length - 1;
        for (int i = 0; i < verts.Length; i++)
        {
            if ((verts[i].y > py) != (verts[j].y > py) &&
                px < (verts[j].x - verts[i].x) * (py - verts[i].y) / (verts[j].y - verts[i].y) + verts[i].x)
            {
                inside = !inside;
            }
            j = i;
        }
        return inside;
    }

    // ─── GAME OBJECT BUILDERS ─────────────────────────────────

    /// <summary>Make a 2D sprite GameObject with optional physics.</summary>
    public static GameObject Make(string name, Sprite sprite, Color color, Vector3 pos, Vector2 scale,
        bool addPhysics = false, bool isStatic = false, int sortOrder = 0, Transform parent = null)
    {
        GameObject go = new GameObject(name);
        if (parent != null) go.transform.SetParent(parent);
        go.transform.localPosition = pos;
        go.transform.localScale = new Vector3(scale.x, scale.y, 1f);

        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.color = color;
        sr.sortingOrder = sortOrder;

        if (addPhysics || isStatic)
        {
            // Add collider matching shape
            if (sprite == Circle())
                go.AddComponent<CircleCollider2D>();
            else
                go.AddComponent<BoxCollider2D>();

            if (addPhysics && !isStatic)
            {
                Rigidbody2D rb = go.AddComponent<Rigidbody2D>();
                rb.gravityScale = 1f;
            }
        }

        return go;
    }

    /// <summary>Quick square with physics.</summary>
    public static GameObject MakeSquare(Vector3 pos, float size, Color color, Transform parent = null, bool physics = true)
    {
        return Make("Square", Square(), color, pos, Vector2.one * size, physics, false, 3, parent);
    }

    /// <summary>Quick circle with physics.</summary>
    public static GameObject MakeCircle(Vector3 pos, float size, Color color, Transform parent = null, bool physics = true)
    {
        return Make("Circle", Circle(), color, pos, Vector2.one * size, physics, false, 3, parent);
    }

    /// <summary>Quick triangle with physics.</summary>
    public static GameObject MakeTriangle(Vector3 pos, float size, Color color, Transform parent = null, bool physics = true)
    {
        return Make("Triangle", Triangle(), color, pos, Vector2.one * size, physics, false, 3, parent);
    }

    /// <summary>Quick static platform (box).</summary>
    public static GameObject MakePlatform(Vector3 pos, float width, float height, Color color, Transform parent = null, int sort = 0)
    {
        return Make("Platform", Square(), color, pos, new Vector2(width, height), false, true, sort, parent);
    }
}
