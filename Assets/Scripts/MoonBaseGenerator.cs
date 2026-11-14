using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

[ExecuteAlways, DisallowMultipleComponent, DefaultExecutionOrder(-900)]
public class MoonBaseGenerator : MonoBehaviour
{
    public MoonWorldGenerator terrain;
    public Material structureMaterial;
    public Material darkMaterial;
    public Material solarMaterial;
    public Material accentMaterial;
    public bool previewInEditor = true;
    public int layoutSeed = 42;
    [SerializeField] MoonBaseState inventory = new MoonBaseState();
    MoonBaseServices services;
    GameObject generated;
    Material glassMaterial;
    bool dirty = true;
    Vector2 lastCenter;
    float lastHeight;

    void OnEnable()
    {
        dirty = true;
#if UNITY_EDITOR
        EditorApplication.update += EditorTick;
#endif
    }
    void OnDisable()
    {
#if UNITY_EDITOR
        EditorApplication.update -= EditorTick;
#endif
        Clear();
    }
    void OnDestroy() { Clear(); }
    void OnValidate() { dirty = true; }
    [ContextMenu("Regenerate Main Base")]
    public void Regenerate() { dirty = true; }
    void Update() { if (Application.isPlaying) Tick(); }
#if UNITY_EDITOR
    void EditorTick()
    {
        if (Application.isPlaying || this == null || !isActiveAndEnabled || EditorApplication.isCompiling || EditorApplication.isUpdating) return;
        if (EditorUtility.IsPersistent(gameObject) || PrefabStageUtility.GetPrefabStage(gameObject) != null) return;
        Tick();
    }
#endif
    void Tick()
    {
        if (!Application.isPlaying && !previewInEditor) { if (generated != null) Clear(); return; }
        if (terrain == null) terrain = (MoonWorldGenerator)GetComponent(typeof(MoonWorldGenerator));
        if (terrain == null || !terrain.isActiveAndEnabled) { if (generated != null) Clear(); return; }
        if (generated == null || dirty || lastCenter != terrain.baseCenter || !Mathf.Approximately(lastHeight, terrain.baseHeight))
            Generate();
    }
    void Clear()
    {
        if (generated != null)
        {
            generated.SetActive(false);
            if (Application.isPlaying) Destroy(generated); else DestroyImmediate(generated);
            generated = null;
        }
    }

    Material GetGlassMaterial()
    {
        if (glassMaterial != null) return glassMaterial;
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) shader = Shader.Find("Standard");
        glassMaterial = new Material(shader);
        glassMaterial.hideFlags = HideFlags.DontSave;
        glassMaterial.color = new Color(0.62f, 0.84f, 0.98f, 0.24f);
        if (glassMaterial.HasProperty("_BaseColor"))
            glassMaterial.SetColor("_BaseColor", new Color(0.62f, 0.84f, 0.98f, 0.24f));
        if (glassMaterial.HasProperty("_Surface"))
        {
            glassMaterial.SetFloat("_Surface", 1f);
            glassMaterial.SetFloat("_Blend", 0f);
            glassMaterial.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
            glassMaterial.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            glassMaterial.SetFloat("_ZWrite", 0f);
            glassMaterial.renderQueue = 3000;
            glassMaterial.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        }
        if (glassMaterial.HasProperty("_Smoothness")) glassMaterial.SetFloat("_Smoothness", 0.92f);
        return glassMaterial;
    }

    public void Generate()
    {
        if (terrain == null) terrain = (MoonWorldGenerator)GetComponent(typeof(MoonWorldGenerator));
        if (terrain == null) return;
        Clear();
        generated = new GameObject("Main Moon Base (generated)");
        generated.hideFlags = HideFlags.DontSave;
        generated.transform.SetParent(terrain.transform, false);
        float ground = terrain.flattenBaseSite ? terrain.baseHeight : terrain.SampleHeightLocal(terrain.baseCenter.x, terrain.baseCenter.y);
        generated.transform.localPosition = new Vector3(terrain.baseCenter.x, ground, terrain.baseCenter.y);
        lastCenter = terrain.baseCenter; lastHeight = terrain.baseHeight; dirty = false;
        services = (MoonBaseServices)generated.AddComponent(typeof(MoonBaseServices));
        services.Initialize(terrain.player, inventory);

        Box("Foundation plaza", new Vector3(0, -0.10f, 2f), new Vector3(46f, 0.36f, 44f), darkMaterial);

        Vector3 mainPos = new Vector3(0f, 0f, 12f);
        Vector3 kitchenPos = new Vector3(-14.5f, 0f, 12f);
        Vector3 livingPos = new Vector3(14.5f, 0f, 12f);

        // 1. Main Communications Dome
        var commandConsole = BuildDome("Command Dome", mainPos, 7.2f, 6.2f, true, true, true);
        Station(commandConsole, BaseStation.UseKind.Command, "check base status", 0.5f);
        RoofDishAntenna(mainPos + new Vector3(0f, 6.1f, 0f));

        // 2. Left Dome: Kitchen
        BuildDome("Kitchen Dome", kitchenPos, 4.6f, 4.2f, false, false, true);

        // 3. Right Dome: Bedroom and Gym
        BuildDome("Living & Gym Dome", livingPos, 4.6f, 4.2f, false, true, false);

        // Pressurized Corrugated Tunnels
        BuildTunnel("Kitchen Tunnel", new Vector3(-8.45f, 0f, 12f), 3.3f);
        BuildTunnel("Living Tunnel", new Vector3(8.45f, 0f, 12f), 3.3f);

        // Exterior Infrastructure & Interiors
        Walkway("Command access", new Vector3(0f, 0.10f, -2f), new Vector3(3.2f, 0.12f, 12f));
        LandingPad(new Vector3(13f, 0f, -11f));
        SolarFarm();
        Utilities();
        FunctionalInteriors(kitchenPos, livingPos);
        BuildInteriorDetails(mainPos, kitchenPos, livingPos);
        Box("South approach", new Vector3(0f, -0.015f, -22f), new Vector3(5f, 0.15f, 6f), darkMaterial);

#if UNITY_EDITOR
        if (!Application.isPlaying) SceneView.RepaintAll();
#endif
        if (Application.isPlaying) Physics.SyncTransforms();
    }

    GameObject BuildDome(string title, Vector3 center, float radius, float height, bool hasFrontAirlock, bool openLeft, bool openRight)
    {
        Part(PrimitiveType.Cylinder, title + " floor", center + new Vector3(0f, 0.12f, 0f), new Vector3(radius * 2f, 0.16f, radius * 2f), darkMaterial);

        int wallSegments = 16;
        float wallHeight = 2.6f;
        float segArc = 2f * Mathf.PI * radius / wallSegments;
        for (int i = 0; i < wallSegments; i++)
        {
            float angleDeg = i * (360f / wallSegments);
            float rad = angleDeg * Mathf.Deg2Rad;
            Vector3 dir = new Vector3(Mathf.Sin(rad), 0f, Mathf.Cos(rad));

            if (hasFrontAirlock && Mathf.Abs(Mathf.DeltaAngle(angleDeg, 180f)) < 24f) continue;
            if (openLeft && Mathf.Abs(Mathf.DeltaAngle(angleDeg, 270f)) < 24f) continue;
            if (openRight && Mathf.Abs(Mathf.DeltaAngle(angleDeg, 90f)) < 24f) continue;

            var wall = Box(title + " base wall " + i, center + dir * (radius - 0.12f) + Vector3.up * 0.65f, new Vector3(segArc * 1.05f, 1.3f, 0.28f), structureMaterial, true);
            wall.transform.localRotation = Quaternion.Euler(0f, angleDeg, 0f);

            var glassCol = Box(title + " glass collider " + i, center + dir * (radius - 0.12f) + Vector3.up * (1.3f + wallHeight * 0.5f), new Vector3(segArc * 1.05f, wallHeight, 0.25f), structureMaterial, true);
            glassCol.transform.localRotation = Quaternion.Euler(0f, angleDeg, 0f);
            ((Renderer)glassCol.GetComponent(typeof(Renderer))).enabled = false;
        }

        int ribs = 8;
        for (int r = 0; r < ribs; r++)
        {
            float yaw = r * (180f / ribs);
            CreateArchRib(title + " rib " + r, center, radius, height, yaw);
        }

        CreateGlassDomeMesh(title + " glass dome", center + Vector3.up * 0.2f, radius * 0.99f, height - 0.2f);

        var lampObject = new GameObject(title + " lamp");
        lampObject.hideFlags = HideFlags.DontSave;
        lampObject.transform.SetParent(generated.transform, false);
        lampObject.transform.localPosition = center + new Vector3(0f, height * 0.72f, 0f);
        var lamp = (Light)lampObject.AddComponent(typeof(Light));
        lamp.type = LightType.Point;
        lamp.color = new Color(0.88f, 0.94f, 1f);
        lamp.range = radius * 2.2f;
        lamp.intensity = 2.0f;
        lamp.shadows = LightShadows.None;
        services.RegisterLamp(lamp);

        if (hasFrontAirlock)
        {
            Vector3 airlockCenter = center + new Vector3(0f, 0f, -radius - 0.7f);
            Box(title + " airlock floor", airlockCenter + Vector3.up * 0.12f, new Vector3(3.4f, 0.16f, 2.2f), darkMaterial);
            Box(title + " airlock roof", airlockCenter + Vector3.up * 3.1f, new Vector3(3.6f, 0.25f, 2.4f), structureMaterial);
            Box(title + " airlock wall L", airlockCenter + new Vector3(-1.65f, 1.55f, 0f), new Vector3(0.25f, 3.1f, 2.2f), structureMaterial);
            Box(title + " airlock wall R", airlockCenter + new Vector3(1.65f, 1.55f, 0f), new Vector3(0.25f, 3.1f, 2.2f), structureMaterial);

            float doorWidth = 2.6f;
            float frontZ = airlockCenter.z - 1.0f;
            Box(title + " lintel", new Vector3(0f, 2.85f, frontZ), new Vector3(3.4f, 0.55f, 0.25f), structureMaterial);
            Box(title + " marker L", new Vector3(-doorWidth * 0.5f - 0.12f, 1.35f, frontZ - 0.12f), new Vector3(0.14f, 2.5f, 0.12f), accentMaterial, false);
            Box(title + " marker R", new Vector3(doorWidth * 0.5f + 0.12f, 1.35f, frontZ - 0.12f), new Vector3(0.14f, 2.5f, 0.12f), accentMaterial, false);

            var doorAnchor = new GameObject(title + " automatic door");
            doorAnchor.hideFlags = HideFlags.DontSave;
            doorAnchor.transform.SetParent(generated.transform, false);
            doorAnchor.transform.localPosition = new Vector3(0f, 0.2f, frontZ);
            var left = Box(title + " door L", new Vector3(-doorWidth * 0.25f, 1.4f, frontZ), new Vector3(doorWidth * 0.5f - 0.05f, 2.5f, 0.16f), structureMaterial);
            var right = Box(title + " door R", new Vector3(doorWidth * 0.25f, 1.4f, frontZ), new Vector3(doorWidth * 0.5f - 0.05f, 2.5f, 0.16f), structureMaterial);
            var doorComp = (BaseDoor)doorAnchor.AddComponent(typeof(BaseDoor));
            doorComp.Configure(terrain.player, services, left.transform, right.transform);

            var console = Box(title + " comm console", center + new Vector3(0f, 0.75f, 2.2f), new Vector3(3.2f, 1.2f, 0.9f), darkMaterial);
            Box(title + " comm screen 1", center + new Vector3(-0.8f, 1.6f, 2.15f), new Vector3(1.2f, 0.75f, 0.1f), solarMaterial, false);
            Box(title + " comm screen 2", center + new Vector3(0.8f, 1.6f, 2.15f), new Vector3(1.2f, 0.75f, 0.1f), solarMaterial, false);
            Box(title + " comm desk L", center + new Vector3(-2.6f, 0.65f, 1.4f), new Vector3(1.6f, 0.9f, 0.8f), structureMaterial);
            Box(title + " comm desk R", center + new Vector3(2.6f, 0.65f, 1.4f), new Vector3(1.6f, 0.9f, 0.8f), structureMaterial);
            return console;
        }
        return null;
    }

    void CreateArchRib(string title, Vector3 center, float radius, float height, float yawDeg)
    {
        int segments = 12;
        for (int s = 0; s < segments; s++)
        {
            float t0 = (float)s / segments * Mathf.PI;
            float t1 = (float)(s + 1) / segments * Mathf.PI;

            Vector3 p0 = new Vector3(Mathf.Cos(t0) * radius, Mathf.Sin(t0) * height, 0f);
            Vector3 p1 = new Vector3(Mathf.Cos(t1) * radius, Mathf.Sin(t1) * height, 0f);
            Vector3 pm = (p0 + p1) * 0.5f;
            float len = Vector3.Distance(p0, p1);

            Quaternion rotY = Quaternion.Euler(0f, yawDeg, 0f);
            var seg = Box(title + "_" + s, center + rotY * pm, new Vector3(len * 1.04f, 0.14f, 0.16f), structureMaterial, false);
            Vector3 tangent = rotY * (p1 - p0).normalized;
            Vector3 side = Vector3.Cross(tangent, Vector3.up);
            if (side.sqrMagnitude > 0.001f)
            {
                Quaternion look = Quaternion.LookRotation(side, Vector3.up);
                seg.transform.localRotation = look * Quaternion.FromToRotation(Vector3.right, Quaternion.Inverse(look) * tangent);
            }
        }
    }

    void CreateGlassDomeMesh(string title, Vector3 center, float radius, float height)
    {
        var obj = new GameObject(title);
        obj.hideFlags = HideFlags.DontSave;
        obj.layer = gameObject.layer;
        obj.transform.SetParent(generated.transform, false);
        obj.transform.localPosition = center;

        var mf = (MeshFilter)obj.AddComponent(typeof(MeshFilter));
        var mr = (MeshRenderer)obj.AddComponent(typeof(MeshRenderer));
        mr.sharedMaterial = GetGlassMaterial();

        int rings = 10;
        int sectors = 24;
        int vertCount = (rings + 1) * (sectors + 1);
        Vector3[] verts = new Vector3[vertCount];
        Vector3[] norms = new Vector3[vertCount];
        int[] tris = new int[rings * sectors * 6];

        int vIdx = 0;
        for (int r = 0; r <= rings; r++)
        {
            float phi = (float)r / rings * (Mathf.PI * 0.5f);
            float y = Mathf.Sin(phi) * height;
            float ringRad = Mathf.Cos(phi) * radius;
            for (int s = 0; s <= sectors; s++)
            {
                float theta = (float)s / sectors * Mathf.PI * 2f;
                float x = Mathf.Cos(theta) * ringRad;
                float z = Mathf.Sin(theta) * ringRad;
                verts[vIdx] = new Vector3(x, y, z);
                norms[vIdx] = new Vector3(x / radius, y / height, z / radius).normalized;
                vIdx++;
            }
        }

        int tIdx = 0;
        int stride = sectors + 1;
        for (int r = 0; r < rings; r++)
        {
            for (int s = 0; s < sectors; s++)
            {
                int a = r * stride + s;
                int b = a + stride;
                int c = a + 1;
                int d = b + 1;
                tris[tIdx++] = a;
                tris[tIdx++] = c;
                tris[tIdx++] = b;
                tris[tIdx++] = b;
                tris[tIdx++] = c;
                tris[tIdx++] = d;
            }
        }

        var mesh = new Mesh { name = title + "Mesh" };
        mesh.vertices = verts;
        mesh.normals = norms;
        mesh.triangles = tris;
        mf.sharedMesh = mesh;
    }

    void BuildTunnel(string title, Vector3 center, float length)
    {
        float w = 2.6f;
        float h = 2.5f;
        Box(title + " floor", center + new Vector3(0f, 0.12f, 0f), new Vector3(length, 0.16f, w), darkMaterial);
        Box(title + " roof", center + new Vector3(0f, h, 0f), new Vector3(length, 0.18f, w), structureMaterial);
        Box(title + " wall N", center + new Vector3(0f, h * 0.5f, w * 0.5f), new Vector3(length, h, 0.2f), structureMaterial);
        Box(title + " wall S", center + new Vector3(0f, h * 0.5f, -w * 0.5f), new Vector3(length, h, 0.2f), structureMaterial);

        int rings = 5;
        for (int i = 0; i < rings; i++)
        {
            float offsetX = Mathf.Lerp(-length * 0.42f, length * 0.42f, (float)i / (rings - 1));
            Box(title + " ring " + i, center + new Vector3(offsetX, h * 0.5f + 0.05f, 0f), new Vector3(0.22f, h + 0.24f, w + 0.28f), accentMaterial, false);
        }
    }

    void RoofDishAntenna(Vector3 p)
    {
        Part(PrimitiveType.Cylinder, "Main Roof Mast", p + Vector3.up * 0.9f, new Vector3(0.45f, 0.9f, 0.45f), darkMaterial, false);
        var dish = Part(PrimitiveType.Sphere, "Main Roof Dish", p + Vector3.up * 2.3f, new Vector3(3.8f, 0.45f, 3.8f), structureMaterial, false);
        dish.transform.localRotation = Quaternion.Euler(32f, -25f, 0f);
        Part(PrimitiveType.Cylinder, "Dish Feed Horn", p + new Vector3(-0.3f, 2.8f, 0.5f), new Vector3(0.14f, 0.7f, 0.14f), accentMaterial, false);
    }

    GameObject Part(PrimitiveType type, string title, Vector3 position, Vector3 scale, Material material, bool solid = true)
    {
        var obj = GameObject.CreatePrimitive(type);
        obj.name = title;
        obj.hideFlags = HideFlags.DontSave;
        obj.layer = gameObject.layer;
        obj.transform.SetParent(generated.transform, false);
        obj.transform.localPosition = position;
        obj.transform.localScale = scale;
        ((Renderer)obj.GetComponent(typeof(Renderer))).sharedMaterial = material;
        var collider = (Collider)obj.GetComponent(typeof(Collider));
        if (!solid && collider != null)
        {
            collider.enabled = false;
        }
        return obj;
    }

    GameObject Box(string title, Vector3 p, Vector3 size, Material material, bool solid = true)
    {
        return Part(PrimitiveType.Cube, title, p, size, material, solid);
    }

    void Walkway(string title, Vector3 p, Vector3 size)
    {
        Box(title, p, size, darkMaterial);
        Box(title + " edge L", p + new Vector3(-size.x * 0.5f + 0.06f, 0.07f, 0f), new Vector3(0.12f, 0.025f, size.z), accentMaterial, false);
        Box(title + " edge R", p + new Vector3(size.x * 0.5f - 0.06f, 0.07f, 0f), new Vector3(0.12f, 0.025f, size.z), accentMaterial, false);
    }

    void LandingPad(Vector3 p)
    {
        Part(PrimitiveType.Cylinder, "Landing pad", p + Vector3.up * 0.105f, new Vector3(13f, 0.025f, 13f), darkMaterial, false);
        Box("Landing H left", p + new Vector3(-1.2f, 0.14f, 0f), new Vector3(0.35f, 0.02f, 4f), accentMaterial, false);
        Box("Landing H right", p + new Vector3(1.2f, 0.14f, 0f), new Vector3(0.35f, 0.02f, 4f), accentMaterial, false);
        Box("Landing H bridge", p + new Vector3(0f, 0.14f, 0f), new Vector3(2.4f, 0.02f, 0.35f), accentMaterial, false);
    }

    void SolarFarm()
    {
        for (int row = 0; row < 2; row++)
        {
            for (int col = 0; col < 2; col++)
            {
                Vector3 p = new Vector3(-26f + col * 6.5f, 0f, -6f + row * 7f);
                Box("Solar support", p + Vector3.up * 0.9f, new Vector3(0.25f, 1.8f, 0.25f), darkMaterial);
                var panel = Box("Solar array", p + Vector3.up * 2f, new Vector3(4.5f, 0.15f, 4.8f), solarMaterial);
                panel.transform.localRotation = Quaternion.Euler(22f, 0f, 0f);
            }
        }
    }

    void Utilities()
    {
        var random = new System.Random(layoutSeed);
        for (int i = 0; i < 5; i++)
        {
            Vector3 p = new Vector3(-16f + i * 2.2f, 0.65f, -15f);
            var crate = Box("Supply container " + (i + 1), p, new Vector3(1.7f, 1.1f, 1.5f), structureMaterial);
            crate.transform.localRotation = Quaternion.Euler(0f, (float)random.NextDouble() * 12f - 6f, 0f);
            var station = Station(crate, BaseStation.UseKind.Supply, "open supply container", 1.2f, i);
            station.usedMarker = Box("Opened container marker", p + Vector3.up * 0.58f, new Vector3(1.2f, 0.05f, 0.4f), accentMaterial, false);
            station.usedMarker.SetActive(inventory.openedCrates[i]);
        }
        for (int i = 0; i < 3; i++)
        {
            Vector3 p = new Vector3(21f, 1.5f, -2f + i * 2.8f);
            Part(PrimitiveType.Cylinder, "Oxygen storage " + (i + 1), p, new Vector3(1.4f, 1.5f, 1.4f), structureMaterial);
        }
    }

    BaseStation Station(GameObject obj, BaseStation.UseKind kind, string label, float seconds, int index = -1)
    {
        var station = (BaseStation)obj.AddComponent(typeof(BaseStation));
        station.Configure(services, kind, label, seconds, index);
        return station;
    }

    void FunctionalInteriors(Vector3 kitchenPos, Vector3 livingPos)
    {
        // --- KITCHEN DOME (Left - Solo Setup) ---
        var water = Part(PrimitiveType.Cylinder, "Water dispenser", kitchenPos + new Vector3(-2.3f, 1.0f, 1.4f), new Vector3(0.75f, 1.0f, 0.75f), structureMaterial);
        Station(water, BaseStation.UseKind.Water, "refill water (+40)", 1.3f);
        Box("Water dispenser tap", kitchenPos + new Vector3(-2.3f, 0.95f, 0.95f), new Vector3(0.25f, 0.12f, 0.3f), solarMaterial, false);

        var food = Box("Food counter", kitchenPos + new Vector3(-0.3f, 0.75f, 2.5f), new Vector3(2.2f, 1.2f, 0.9f), structureMaterial);
        Station(food, BaseStation.UseKind.Food, "take ration (+35)", 1.6f);
        Box("Kitchen upper cabinet", kitchenPos + new Vector3(-0.3f, 2.1f, 2.7f), new Vector3(2.0f, 0.6f, 0.45f), darkMaterial, false);

        Box("Dining table", kitchenPos + new Vector3(-0.6f, 0.55f, -0.8f), new Vector3(1.25f, 0.12f, 0.9f), structureMaterial);
        Box("Dining table leg", kitchenPos + new Vector3(-0.6f, 0.28f, -0.8f), new Vector3(0.22f, 0.55f, 0.22f), darkMaterial, false);
        Part(PrimitiveType.Cylinder, "Dining stool", kitchenPos + new Vector3(-0.6f, 0.32f, -1.55f), new Vector3(0.55f, 0.22f, 0.55f), darkMaterial);

        // --- LIVING & GYM DOME (Right - Solo Setup) ---
        var bed = Box("Rest bunk", livingPos + new Vector3(-0.8f, 0.45f, 2.2f), new Vector3(1.4f, 0.65f, 2.4f), darkMaterial);
        Box("Rest mattress", livingPos + new Vector3(-0.8f, 0.82f, 2.2f), new Vector3(1.28f, 0.12f, 2.25f), structureMaterial, false);
        Station(bed, BaseStation.UseKind.Rest, "rest (+40 sanity)", 3f);

        var med = Box("Medical station", livingPos + new Vector3(1.8f, 0.85f, 1.9f), new Vector3(1.3f, 1.4f, 0.9f), structureMaterial);
        Station(med, BaseStation.UseKind.Medical, "treat injuries and fractures", 3f);
        Box("Medical cross horizontal", livingPos + new Vector3(1.8f, 1.35f, 1.42f), new Vector3(0.65f, 0.15f, 0.03f), accentMaterial, false);
        Box("Medical cross vertical", livingPos + new Vector3(1.8f, 1.35f, 1.41f), new Vector3(0.15f, 0.65f, 0.03f), accentMaterial, false);

        // Solo Gym Area (Interactive Treadmill -> restores Sanity)
        var treadmill = Box("Treadmill deck", livingPos + new Vector3(-0.5f, 0.25f, -2.1f), new Vector3(0.95f, 0.18f, 1.9f), darkMaterial);
        Station(treadmill, BaseStation.UseKind.Workout, "work out (+35 sanity)", 2.5f);
        Box("Treadmill console", livingPos + new Vector3(-0.5f, 1.05f, -2.95f), new Vector3(0.85f, 1.35f, 0.22f), structureMaterial);
        Box("Treadmill screen", livingPos + new Vector3(-0.5f, 1.45f, -2.82f), new Vector3(0.55f, 0.35f, 0.05f), solarMaterial, false);

        Box("Workout bench", livingPos + new Vector3(1.85f, 0.32f, -2.1f), new Vector3(0.55f, 0.42f, 1.45f), darkMaterial);
        Box("Dumbbell handle L", livingPos + new Vector3(1.4f, 0.25f, -2.4f), new Vector3(0.32f, 0.06f, 0.06f), accentMaterial, false);
        Box("Dumbbell handle R", livingPos + new Vector3(1.4f, 0.25f, -1.8f), new Vector3(0.32f, 0.06f, 0.06f), accentMaterial, false);

        var power = Box("Base power cabinet", new Vector3(-18.5f, 1.05f, 4f), new Vector3(0.9f, 1.8f, 0.5f), darkMaterial);
        Station(power, BaseStation.UseKind.Power, "toggle base power", 0.8f);
        var inverter = Box("Solar inverter", new Vector3(-22f, 0.7f, 1f), new Vector3(1.1f, 1.3f, 0.9f), darkMaterial);
        Station(inverter, BaseStation.UseKind.Solar, "toggle solar panel charging", 0.8f);
    }

    void BuildInteriorDetails(Vector3 mainPos, Vector3 kitchenPos, Vector3 livingPos)
    {
        // ==========================================
        // 1. CENTRAL COMMAND DOME (Solo Operator)
        // ==========================================
        Part(PrimitiveType.Cylinder, "Holo table base", mainPos + new Vector3(0f, 0.45f, -1.2f), new Vector3(1.5f, 0.35f, 1.5f), darkMaterial, true);
        Part(PrimitiveType.Cylinder, "Holo table rim", mainPos + new Vector3(0f, 0.82f, -1.2f), new Vector3(1.7f, 0.06f, 1.7f), structureMaterial, false);
        Part(PrimitiveType.Cylinder, "Holo projection", mainPos + new Vector3(0f, 0.92f, -1.2f), new Vector3(1.35f, 0.02f, 1.35f), solarMaterial, false);

        for (int side = -1; side <= 1; side += 2)
        {
            Vector3 rackPos = mainPos + new Vector3(side * 3.4f, 1.25f, 3.4f);
            Box("Server rack " + side, rackPos, new Vector3(1.1f, 2.2f, 0.8f), darkMaterial, true);
            Box("Server stripe A " + side, rackPos + new Vector3(0f, 0.55f, -0.41f), new Vector3(0.8f, 0.08f, 0.02f), solarMaterial, false);
            Box("Server stripe B " + side, rackPos + new Vector3(0f, 0.15f, -0.41f), new Vector3(0.8f, 0.08f, 0.02f), accentMaterial, false);
        }

        Vector3 chairPos = mainPos + new Vector3(0f, 0.35f, 1.15f);
        Part(PrimitiveType.Cylinder, "Operator chair base", chairPos, new Vector3(0.48f, 0.25f, 0.48f), darkMaterial, false);
        Box("Operator chair seat", chairPos + Vector3.up * 0.28f, new Vector3(0.58f, 0.1f, 0.58f), structureMaterial, false);
        Box("Operator chair back", chairPos + Vector3.up * 0.65f + Vector3.back * 0.25f, new Vector3(0.54f, 0.62f, 0.1f), structureMaterial, false);

        // ==========================================
        // 2. KITCHEN DOME (Solo Astronaut)
        // ==========================================
        Box("Kitchen fridge", kitchenPos + new Vector3(1.65f, 1.15f, 2.35f), new Vector3(1.05f, 2.0f, 0.9f), structureMaterial, true);
        Box("Fridge handle", kitchenPos + new Vector3(1.25f, 1.15f, 1.88f), new Vector3(0.06f, 0.65f, 0.08f), accentMaterial, false);

        Box("Hydroponics frame", kitchenPos + new Vector3(-2.65f, 1.05f, -1.2f), new Vector3(0.75f, 1.85f, 1.8f), darkMaterial, true);
        for (int shelf = 0; shelf < 3; shelf++)
        {
            float sy = 0.55f + shelf * 0.55f;
            Box("Hydro tray " + shelf, kitchenPos + new Vector3(-2.65f, sy, -1.2f), new Vector3(0.65f, 0.12f, 1.65f), structureMaterial, false);
            Box("Hydro plants " + shelf, kitchenPos + new Vector3(-2.65f, sy + 0.12f, -1.2f), new Vector3(0.52f, 0.14f, 1.5f), solarMaterial, false);
        }

        Part(PrimitiveType.Cylinder, "Solo plate", kitchenPos + new Vector3(-0.6f, 0.63f, -1.0f), new Vector3(0.32f, 0.02f, 0.32f), structureMaterial, false);
        Part(PrimitiveType.Cylinder, "Solo mug", kitchenPos + new Vector3(-0.32f, 0.68f, -1.05f), new Vector3(0.11f, 0.07f, 0.11f), accentMaterial, false);

        // ==========================================
        // 3. LIVING & GYM DOME (Solo Astronaut)
        // ==========================================
        Box("Solo pillow", livingPos + new Vector3(-0.8f, 0.93f, 3.05f), new Vector3(0.85f, 0.14f, 0.45f), accentMaterial, false);
        Box("Nightstand", livingPos + new Vector3(-1.95f, 0.35f, 2.85f), new Vector3(0.55f, 0.55f, 0.55f), structureMaterial, true);
        Box("Personal tablet", livingPos + new Vector3(-1.95f, 0.65f, 2.85f), new Vector3(0.28f, 0.03f, 0.2f), solarMaterial, false);

        Box("Crew locker", livingPos + new Vector3(-2.55f, 1.05f, 1.45f), new Vector3(0.85f, 1.85f, 0.85f), structureMaterial, true);
        Box("Locker vent", livingPos + new Vector3(-2.11f, 1.55f, 1.45f), new Vector3(0.03f, 0.35f, 0.45f), darkMaterial, false);

        // Interactive Exercise Bike -> restores Sanity
        var bike = Box("Exercise bike base", livingPos + new Vector3(-2.1f, 0.25f, -1.8f), new Vector3(0.55f, 0.14f, 1.25f), darkMaterial, true);
        Station(bike, BaseStation.UseKind.Workout, "ride exercise bike (+35 sanity)", 2.5f);
        Part(PrimitiveType.Cylinder, "Bike flywheel", livingPos + new Vector3(-2.1f, 0.58f, -2.15f), new Vector3(0.52f, 0.08f, 0.52f), structureMaterial, false);
        Box("Bike handlebar post", livingPos + new Vector3(-2.1f, 0.85f, -2.2f), new Vector3(0.12f, 0.85f, 0.12f), darkMaterial, false);
        Box("Bike seat", livingPos + new Vector3(-2.1f, 0.82f, -1.45f), new Vector3(0.28f, 0.1f, 0.34f), accentMaterial, false);

        Box("Exercise mat", livingPos + new Vector3(0.65f, 0.21f, -1.45f), new Vector3(0.85f, 0.03f, 1.65f), accentMaterial, false);
    }
}