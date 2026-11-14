using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

// Unity 2021.3+ / Unity 6. One component per generated world.
[ExecuteAlways, DisallowMultipleComponent, DefaultExecutionOrder(-1000)]
public class MoonWorldGenerator : MonoBehaviour
{
    [Header("References")]
    public Transform player;
    public Material moonMaterial;
    [Header("Streaming (metres in local space)")]
    public int seed = 12345;
    [Min(16)] public float chunkSize = 128;
    [Min(16)] public float viewDistance = 1536;
    [Min(0)] public float nearDistance = 256;
    [Min(0)] public float mediumDistance = 640;
    [Min(1)] public float colliderDistance = 192;
    [Range(1, 8)] public int chunksPerFrame = 2;
    [Header("Mesh subdivisions (rounded to powers of two)")]
    [Range(8, 128)] public int nearResolution = 64;
    [Range(4, 64)] public int mediumResolution = 32;
    [Range(2, 32)] public int farResolution = 8;
    [Min(1)] public float skirtDepth = 60;
    [Min(0.1f)] public float textureScale = 20;
    [Header("Terrain")]
    [Min(50)] public float regionScale = 2400;
    [Min(0)] public float hillHeight = 18;
    [Min(0)] public float mountainHeight = 240;
    [Min(50)] public float ridgeScale = 700;
    [Min(0)] public float detailHeight = 0.7f;
    [Header("Craters")]
    [Min(32)] public float craterSpacing = 220;
    [Min(2)] public float craterMinRadius = 18;
    [Min(2)] public float craterMaxRadius = 85;
    [Range(0, 1)] public float craterDensity = 0.65f;
    [Range(0, 0.5f)] public float craterDepthRatio = 0.18f;
    [Header("Editor preview")]
    public bool previewInEditor = true;
    public bool followSceneView = true;
    public Transform previewCenter;
    [Min(16)] public float previewDistance = 768;

    [Header("Main base foundation")]
    public bool flattenBaseSite = true;
    public Vector2 baseCenter = new Vector2(-1.34963f, 17.68174f);
    public float baseHeight = -4f;
    [Min(16)] public float baseFlatRadius = 52f;
    [Min(1)] public float baseBlendWidth = 28f;

    // Uses exactly the same deterministic height function as visual/physics chunks.
    public float SampleHeightLocal(float x, float z)
    {
        Sanitize();
        var key = new Vector2Int(Mathf.FloorToInt(x / chunkSize), Mathf.FloorToInt(z / chunkSize));
        return Height(x, z, Craters(key));
    }

    sealed class Chunk
    {
        public GameObject obj;
        public MeshFilter filter;
        public MeshRenderer renderer;
        public MeshCollider collider;
        public Mesh visual, physics;
        public int lod = -1;
    }
    struct Request { public Vector2Int key; public int lod; public float distance; }
    struct Crater { public Vector2 center; public float radius; }
    readonly Dictionary<Vector2Int, Chunk> chunks = new Dictionary<Vector2Int, Chunk>();
    readonly Queue<Request> queue = new Queue<Request>();
    readonly HashSet<Vector2Int> wanted = new HashSet<Vector2Int>();
    GameObject root;
    Vector2 lastCenter = new Vector2(float.PositiveInfinity, 0);
    float lastRadius = -1;
    bool dirty = true;
    bool wasPlaying;
#if UNITY_EDITOR
    double nextEditorTick;
#endif

    void OnEnable()
    {
        dirty = true;
        wasPlaying = Application.isPlaying;
#if UNITY_EDITOR
        EditorApplication.update += EditorTick;
#endif
    }
    void OnDisable()
    {
#if UNITY_EDITOR
        EditorApplication.update -= EditorTick;
#endif
        ClearGenerated();
    }
    void OnDestroy() { ClearGenerated(); }
    // Validation only schedules work: never create/destroy Unity objects here.
    void OnValidate() { dirty = true; }
    [ContextMenu("Regenerate Moon")]
    public void Regenerate() { dirty = true; }
    [ContextMenu("Clear Generated Moon")]
    public void ClearGenerated()
    {
        foreach (Chunk c in chunks.Values) Release(c);
        chunks.Clear(); queue.Clear(); wanted.Clear();
        Dispose(root); root = null;
        lastRadius = -1;
    }
    void Update() { if (Application.isPlaying) Tick(); }
    void FixedUpdate()
    {
        // Also prepare terrain before physics after a player movement/teleport.
        if (Application.isPlaying) Tick(false);
    }
#if UNITY_EDITOR
    void EditorTick()
    {
        if (Application.isPlaying || this == null || !isActiveAndEnabled) return;
        if (EditorApplication.isCompiling || EditorApplication.isUpdating) return;
        if (EditorApplication.timeSinceStartup < nextEditorTick) return;
        nextEditorTick = EditorApplication.timeSinceStartup + 0.05;
        if (EditorUtility.IsPersistent(gameObject) || PrefabStageUtility.GetPrefabStage(gameObject) != null) return;
        Tick();
        if (queue.Count > 0) SceneView.RepaintAll();
    }
#endif
    void Sanitize()
    {
        chunkSize = Mathf.Max(16, chunkSize);
        viewDistance = Mathf.Clamp(viewDistance, chunkSize, chunkSize * 24);
        previewDistance = Mathf.Clamp(previewDistance, chunkSize, chunkSize * 24);
        colliderDistance = Mathf.Clamp(colliderDistance, 1, viewDistance);
        nearDistance = Mathf.Clamp(nearDistance, 0, viewDistance);
        mediumDistance = Mathf.Clamp(mediumDistance, nearDistance, viewDistance);
        nearResolution = Mathf.Clamp(Mathf.ClosestPowerOfTwo(nearResolution), 8, 128);
        mediumResolution = Mathf.Clamp(Mathf.ClosestPowerOfTwo(mediumResolution), 4, nearResolution);
        farResolution = Mathf.Clamp(Mathf.ClosestPowerOfTwo(farResolution), 2, mediumResolution);
        regionScale = Mathf.Max(50, regionScale); ridgeScale = Mathf.Max(50, ridgeScale);
        craterSpacing = Mathf.Max(32, craterSpacing);
        craterMinRadius = Mathf.Clamp(craterMinRadius, 2, craterSpacing);
        craterMaxRadius = Mathf.Clamp(craterMaxRadius, craterMinRadius, craterSpacing);
        textureScale = Mathf.Max(0.1f, textureScale); skirtDepth = Mathf.Max(1, skirtDepth);
    }
    Vector2 Center()
    {
        Vector3 p = transform.position;
        if (Application.isPlaying) { if (player != null) p = player.position; }
        else
        {
            if (previewCenter != null) p = previewCenter.position;
            else if (player != null) p = player.position;
#if UNITY_EDITOR
            if (followSceneView && SceneView.lastActiveSceneView != null)
                p = SceneView.lastActiveSceneView.camera.transform.position;
#endif
        }
        p = transform.InverseTransformPoint(p);
        return new Vector2(p.x, p.z);
    }
    void Tick(bool buildDistant = true)
    {
        if (!Application.isPlaying && !previewInEditor)
        { if (root != null) ClearGenerated(); return; }
        if (Application.isPlaying && player == null) return;
        if (dirty || wasPlaying != Application.isPlaying)
        {
            ClearGenerated(); Sanitize(); dirty = false; wasPlaying = Application.isPlaying;
        }
        Vector2 center = Center();
        float radius = Application.isPlaying ? viewDistance : previewDistance;
        // Reuse the pending queue between steps instead of sorting the entire
        // world for every 10 cm of movement. Collision coverage has the same margin.
        float refreshStep = RefreshStep();
        if (lastRadius != radius || (center - lastCenter).sqrMagnitude >= refreshStep * refreshStep)
            Refresh(center, radius);
        if (buildDistant)
            for (int i = 0; i < Mathf.Clamp(chunksPerFrame, 1, 8) && queue.Count > 0; i++)
            {
                Request r = queue.Dequeue();
                if (wanted.Contains(r.key)) Build(r.key, r.lod, false);
            }
    }
    float RefreshStep()
    {
        return Mathf.Clamp(Mathf.Min(chunkSize * .0625f, colliderDistance * .125f), .25f, 8f);
    }
    public void PreparePlayerCollision()
    {
        if (Application.isPlaying && isActiveAndEnabled) Tick(false);
    }
    // Minimum XZ distance to the chunk's square. Whole chunks are cut off.
    float Distance(Vector2Int key, Vector2 p)
    {
        float x = Mathf.Max(key.x * chunkSize - p.x, 0, p.x - (key.x + 1) * chunkSize);
        float z = Mathf.Max(key.y * chunkSize - p.y, 0, p.y - (key.y + 1) * chunkSize);
        return Mathf.Sqrt(x * x + z * z);
    }
    void Refresh(Vector2 center, float radius)
    {
        lastCenter = center; lastRadius = radius; queue.Clear(); wanted.Clear();
        var pending = new List<Request>();
        int minX = Mathf.FloorToInt((center.x - radius) / chunkSize);
        int maxX = Mathf.FloorToInt((center.x + radius) / chunkSize);
        int minZ = Mathf.FloorToInt((center.y - radius) / chunkSize);
        int maxZ = Mathf.FloorToInt((center.y + radius) / chunkSize);
        for (int z = minZ; z <= maxZ; z++) for (int x = minX; x <= maxX; x++)
        {
            var key = new Vector2Int(x, z);
            float d = Distance(key, center);
            if (d > radius) continue;
            wanted.Add(key);
            bool collision = Application.isPlaying && d <= colliderDistance + RefreshStep();
            int lod = collision || d <= nearDistance ? 0 : d <= mediumDistance ? 1 : 2;
            if (collision) Build(key, lod, true); // Synchronous safety ring, no distant queue delay.
            else
            {
                if (chunks.TryGetValue(key, out Chunk c))
                {
                    SetCollision(c, false);
                    if (c.lod == lod) continue;
                }
                pending.Add(new Request { key = key, lod = lod, distance = d });
            }
        }
        var remove = new List<Vector2Int>();
        foreach (var pair in chunks) if (!wanted.Contains(pair.Key)) remove.Add(pair.Key);
        foreach (var key in remove) { Release(chunks[key]); chunks.Remove(key); }
        pending.Sort((a, b) => a.distance.CompareTo(b.distance));
        foreach (var r in pending) queue.Enqueue(r);
        if (Application.isPlaying) Physics.SyncTransforms();
#if UNITY_EDITOR
        if (!Application.isPlaying) SceneView.RepaintAll();
#endif
    }
    void Build(Vector2Int key, int lod, bool collision)
    {
        if (root == null)
        {
            root = new GameObject("Moon generated (temporary)");
            root.hideFlags = HideFlags.DontSave;
            root.transform.SetParent(transform, false);
        }
        if (!chunks.TryGetValue(key, out Chunk c))
        {
            c = new Chunk(); c.obj = new GameObject("Moon " + key.x + ", " + key.y);
            c.obj.hideFlags = HideFlags.DontSave;
            c.obj.layer = gameObject.layer;
            c.obj.transform.SetParent(root.transform, false);
            c.obj.transform.localPosition = new Vector3(key.x * chunkSize, 0, key.y * chunkSize);
            c.filter = c.obj.AddComponent<MeshFilter>();
            c.renderer = c.obj.AddComponent<MeshRenderer>();
            c.collider = c.obj.AddComponent<MeshCollider>();
            c.collider.enabled = false;
            chunks.Add(key, c);
        }
        c.renderer.sharedMaterial = moonMaterial;
        if (c.lod != lod)
        {
            Mesh old = c.visual;
            c.visual = MakeMesh(key, Resolution(lod), true);
            c.filter.sharedMesh = c.visual; c.lod = lod;
            Dispose(old);
        }
        if (collision && c.physics == null) c.physics = MakeMesh(key, nearResolution, false);
        SetCollision(c, collision);
    }
    int Resolution(int lod) { return lod == 0 ? nearResolution : lod == 1 ? mediumResolution : farResolution; }
    void SetCollision(Chunk c, bool value)
    {
        if (value)
        {
            if (c.collider.sharedMesh != c.physics) c.collider.sharedMesh = c.physics;
            c.collider.enabled = true;
        }
        else
        {
            c.collider.enabled = false; c.collider.sharedMesh = null;
            Dispose(c.physics); c.physics = null;
        }
    }
    void Release(Chunk c)
    {
        if (c.obj != null) c.obj.SetActive(false);
        if (c.collider != null) c.collider.sharedMesh = null;
        if (c.filter != null) c.filter.sharedMesh = null;
        Dispose(c.visual); Dispose(c.physics); Dispose(c.obj);
    }
    static void Dispose(Object obj)
    {
        if (obj == null) return;
        if (Application.isPlaying) Destroy(obj); else DestroyImmediate(obj);
    }
    float Noise(float x, float z, float scale, int salt)
    {
        float ox = Hash(0, salt, 0) * 8192, oz = Hash(salt, 0, 1) * 8192;
        return Mathf.PerlinNoise(x / scale + ox, z / scale + oz);
    }
    float Hash(int x, int z, int salt)
    {
        unchecked
        {
            uint h = (uint)seed ^ (uint)x * 374761393u ^ (uint)z * 668265263u ^ (uint)salt * 2246822519u;
            h = (h ^ (h >> 13)) * 1274126177u; h ^= h >> 16;
            return (h & 0x00ffffff) / 16777216f;
        }
    }
    List<Crater> Craters(Vector2Int key)
    {
        var result = new List<Crater>();
        float margin = craterMaxRadius * 1.4f + 2;
        int x0 = Mathf.FloorToInt((key.x * chunkSize - margin) / craterSpacing);
        int z0 = Mathf.FloorToInt((key.y * chunkSize - margin) / craterSpacing);
        int x1 = Mathf.FloorToInt(((key.x + 1) * chunkSize + margin) / craterSpacing);
        int z1 = Mathf.FloorToInt(((key.y + 1) * chunkSize + margin) / craterSpacing);
        for (int z = z0; z <= z1; z++) for (int x = x0; x <= x1; x++)
            if (Hash(x, z, 2) < craterDensity)
                result.Add(new Crater {
                    center = new Vector2((x + Hash(x, z, 3)) * craterSpacing, (z + Hash(x, z, 4)) * craterSpacing),
                    radius = Mathf.Lerp(craterMinRadius, craterMaxRadius, Hash(x, z, 5)) });
        return result;
    }
    float Height(float x, float z, List<Crater> craters)
    {
        float region = Mathf.SmoothStep(0, 1, Mathf.InverseLerp(0.38f, 0.72f, Noise(x, z, regionScale, 10)));
        float wx = x + (Noise(x, z, ridgeScale * 1.7f, 11) - 0.5f) * ridgeScale;
        float wz = z + (Noise(x, z, ridgeScale * 1.7f, 12) - 0.5f) * ridgeScale;
        float ridge = 1 - Mathf.Abs(Noise(wx, wz, ridgeScale, 13) * 2 - 1);
        float h = (Noise(x, z, 450, 14) - 0.5f) * hillHeight;
        h += region * mountainHeight * (Mathf.Pow(ridge, 3) * 0.8f + Noise(x, z, 230, 15) * 0.2f);
        h += (Noise(x, z, 14, 16) - 0.5f) * detailHeight;
        foreach (Crater c in craters)
        {
            float d = Vector2.Distance(new Vector2(x, z), c.center) / c.radius;
            if (d >= 1.4f) continue;
            float depth = c.radius * craterDepthRatio;
            if (d < 1) { float bowl = 1 - d * d; h -= depth * bowl * bowl; }
            float rim = Mathf.Exp(-Mathf.Pow((d - 1) / 0.12f, 2));
            float fade = 1 - Mathf.SmoothStep(0, 1, Mathf.InverseLerp(1.2f, 1.4f, d));
            h += depth * 0.35f * rim * fade;
        }
        if (flattenBaseSite)
        {
            float distance = Vector2.Distance(new Vector2(x, z), baseCenter);
            float blend = Mathf.Clamp01((distance - Mathf.Max(16f, baseFlatRadius)) / Mathf.Max(1f, baseBlendWidth));
            // Quintic falloff keeps the apron boundary smooth in height and slope.
            blend = blend * blend * blend * (blend * (blend * 6f - 15f) + 10f);
            h = Mathf.Lerp(baseHeight, h, blend);
        }
        return h;
    }
    Mesh MakeMesh(Vector2Int key, int n, bool skirts)
    {
        var craters = Craters(key);
        var vertices = new List<Vector3>(); var normals = new List<Vector3>();
        var uv = new List<Vector2>(); var triangles = new List<int>();
        float step = chunkSize / n;
        for (int z = 0; z <= n; z++) for (int x = 0; x <= n; x++)
        {
            float lx = x * step, lz = z * step;
            float wx = key.x * chunkSize + lx, wz = key.y * chunkSize + lz;
            vertices.Add(new Vector3(lx, Height(wx, wz, craters), lz));
            uv.Add(new Vector2(wx / textureScale, wz / textureScale));
            const float e = 0.5f;
            normals.Add(new Vector3(Height(wx - e, wz, craters) - Height(wx + e, wz, craters),
                2 * e, Height(wx, wz - e, craters) - Height(wx, wz + e, craters)).normalized);
        }
        for (int z = 0; z < n; z++) for (int x = 0; x < n; x++)
        {
            int a = z * (n + 1) + x, b = a + n + 1;
            triangles.Add(a); triangles.Add(b); triangles.Add(a + 1);
            triangles.Add(a + 1); triangles.Add(b); triangles.Add(b + 1);
        }
        // Visual skirts cover cracks between different LODs; never used for physics.
        if (skirts) for (int side = 0; side < 4; side++) for (int i = 0; i < n; i++)
        {
            int a, b;
            if (side == 0) { a = i; b = i + 1; }
            else if (side == 1) { a = i * (n + 1) + n; b = (i + 1) * (n + 1) + n; }
            else if (side == 2) { a = n * (n + 1) + n - i; b = a - 1; }
            else { a = (n - i) * (n + 1); b = a - (n + 1); }
            int k = vertices.Count;
            vertices.Add(vertices[a] - Vector3.up * skirtDepth);
            vertices.Add(vertices[b] - Vector3.up * skirtDepth);
            normals.Add(normals[a]); normals.Add(normals[b]); uv.Add(uv[a]); uv.Add(uv[b]);
            triangles.Add(a); triangles.Add(b); triangles.Add(k);
            triangles.Add(b); triangles.Add(k + 1); triangles.Add(k);
        }
        var mesh = new Mesh { name = "Moon mesh", hideFlags = HideFlags.DontSave, indexFormat = IndexFormat.UInt32 };
        mesh.SetVertices(vertices); mesh.SetNormals(normals); mesh.SetUVs(0, uv); mesh.SetTriangles(triangles, 0);
        mesh.RecalculateTangents(); mesh.RecalculateBounds(); return mesh;
    }
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Vector2 c = Center(); float r = Application.isPlaying ? viewDistance : previewDistance;
        Vector3 previous = transform.TransformPoint(new Vector3(c.x + r, 0, c.y));
        for (int i = 1; i <= 96; i++)
        {
            float a = i * Mathf.PI * 2 / 96;
            Vector3 next = transform.TransformPoint(new Vector3(c.x + Mathf.Cos(a) * r, 0, c.y + Mathf.Sin(a) * r));
            Gizmos.DrawLine(previous, next); previous = next;
        }
    }
}

