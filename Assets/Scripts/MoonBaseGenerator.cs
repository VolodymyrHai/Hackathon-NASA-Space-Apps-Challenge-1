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

        // 1. Центральний купол зв'язку
        var commandConsole = BuildDome("Command Dome", mainPos, 7.2f, 6.2f, true, true, true);
        Station(commandConsole, BaseStation.UseKind.Command, "состояние базы", 0.5f);
        RoofDishAntenna(mainPos + new Vector3(0f, 6.1f, 0f));

        // 2. Лівий купол: Кухня
        BuildDome("Kitchen Dome", kitchenPos, 4.6f, 4.2f, false, false, true);

        // 3. Правий купол: Спальня і спортзал
        BuildDome("Living & Gym Dome", livingPos, 4.6f, 4.2f, false, true, false);

        // Герметичні гофровані тунелі
        BuildTunnel("Kitchen Tunnel", new Vector3(-8.45f, 0f, 12f), 3.3f);
        BuildTunnel("Living Tunnel", new Vector3(8.45f, 0f, 12f), 3.3f);

        // Зовнішня інфраструктура
        Walkway("Command access", new Vector3(0f, 0.10f, -2f), new Vector3(3.2f, 0.12f, 12f));
        LandingPad(new Vector3(13f, 0f, -11f));
        SolarFarm();
        Utilities();
        FunctionalInteriors(kitchenPos, livingPos);
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
tris[tIdx++] = a; tris[tIdx++] = c; tris[tIdx++] = b;
tris[tIdx++] = b; tris[tIdx++] = c; tris[tIdx++] = d;
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
obj.name = title; obj.hideFlags = HideFlags.DontSave; obj.layer = gameObject.layer;
obj.transform.SetParent(generated.transform, false);
obj.transform.localPosition = position; obj.transform.localScale = scale;
((Renderer)obj.GetComponent(typeof(Renderer))).sharedMaterial = material;
var collider = (Collider)obj.GetComponent(typeof(Collider));
if (!solid && collider != null) collider.enabled = false;
return obj;
}
GameObject Box(string title, Vector3 p, Vector3 size, Material material, bool solid = true)
{ return Part(PrimitiveType.Cube, title, p, size, material, solid); }
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
for (int col = 0; col < 2; col++)
{
Vector3 p = new Vector3(-26f + col * 6.5f, 0f, -6f + row * 7f);
Box("Solar support", p + Vector3.up * 0.9f, new Vector3(0.25f, 1.8f, 0.25f), darkMaterial);
var panel = Box("Solar array", p + Vector3.up * 2f, new Vector3(4.5f, 0.15f, 4.8f), solarMaterial);
panel.transform.localRotation = Quaternion.Euler(22f, 0f, 0f);
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
var station = Station(crate, BaseStation.UseKind.Supply, "открыть резервный контейнер", 1.2f, i);
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
// --- КУПОЛ КУХНІ (лівий) ---
var water = Part(PrimitiveType.Cylinder, "Water dispenser", kitchenPos + new Vector3(-2.3f, 1.0f, 1.4f), new Vector3(0.75f, 1.0f, 0.75f), structureMaterial);
Station(water, BaseStation.UseKind.Water, "пополнить воду (+40)", 1.3f);
Box("Water dispenser tap", kitchenPos + new Vector3(-2.3f, 0.95f, 0.95f), new Vector3(0.25f, 0.12f, 0.3f), solarMaterial, false);
var food = Box("Food counter", kitchenPos + new Vector3(-0.3f, 0.75f, 2.5f), new Vector3(2.4f, 1.2f, 0.9f), structureMaterial);
Station(food, BaseStation.UseKind.Food, "взять рацион (+35)", 1.6f);
Box("Kitchen upper cabinet", kitchenPos + new Vector3(-0.3f, 2.1f, 2.7f), new Vector3(2.2f, 0.6f, 0.45f), darkMaterial, false);
Box("Dining table", kitchenPos + new Vector3(-0.6f, 0.55f, -0.8f), new Vector3(1.8f, 0.12f, 1.1f), structureMaterial);
Box("Dining table leg", kitchenPos + new Vector3(-0.6f, 0.28f, -0.8f), new Vector3(0.25f, 0.55f, 0.25f), darkMaterial, false);
Box("Dining bench 1", kitchenPos + new Vector3(-0.6f, 0.32f, -1.65f), new Vector3(1.5f, 0.45f, 0.4f), darkMaterial);
Box("Dining bench 2", kitchenPos + new Vector3(-0.6f, 0.32f, 0.05f), new Vector3(1.5f, 0.45f, 0.4f), darkMaterial);
// --- КУПОЛ СПАЛЬНІ І СПОРТЗАЛУ (правий) ---
var bed = Box("Rest bunk", livingPos + new Vector3(-0.8f, 0.45f, 2.2f), new Vector3(1.6f, 0.65f, 2.5f), darkMaterial);
Box("Rest mattress", livingPos + new Vector3(-0.8f, 0.82f, 2.2f), new Vector3(1.45f, 0.12f, 2.3f), structureMaterial, false);
Box("Upper bunk frame", livingPos + new Vector3(-0.8f, 1.75f, 2.2f), new Vector3(1.55f, 0.12f, 2.4f), structureMaterial, false);
Station(bed, BaseStation.UseKind.Rest, "отдохнуть (+40 к состоянию)", 3f);
var med = Box("Medical station", livingPos + new Vector3(1.8f, 0.85f, 1.9f), new Vector3(1.3f, 1.4f, 0.9f), structureMaterial);
Station(med, BaseStation.UseKind.Medical, "вылечить травмы и переломы", 3f);
Box("Medical cross horizontal", livingPos + new Vector3(1.8f, 1.35f, 1.42f), new Vector3(0.65f, 0.15f, 0.03f), accentMaterial, false);
Box("Medical cross vertical", livingPos + new Vector3(1.8f, 1.35f, 1.41f), new Vector3(0.15f, 0.65f, 0.03f), accentMaterial, false);
Box("Treadmill deck", livingPos + new Vector3(-0.5f, 0.25f, -2.1f), new Vector3(0.95f, 0.18f, 1.9f), darkMaterial);
Box("Treadmill console", livingPos + new Vector3(-0.5f, 1.05f, -2.95f), new Vector3(0.85f, 1.35f, 0.22f), structureMaterial);
Box("Treadmill screen", livingPos + new Vector3(-0.5f, 1.45f, -2.82f), new Vector3(0.55f, 0.35f, 0.05f), solarMaterial, false);
Box("Gym rack L", livingPos + new Vector3(1.6f, 1.1f, -2.3f), new Vector3(0.14f, 2.0f, 0.14f), darkMaterial);
Box("Gym rack R", livingPos + new Vector3(2.8f, 1.1f, -2.3f), new Vector3(0.14f, 2.0f, 0.14f), darkMaterial);
Box("Gym barbell", livingPos + new Vector3(2.2f, 1.45f, -2.3f), new Vector3(1.7f, 0.08f, 0.08f), accentMaterial, false);
var power = Box("Base power cabinet", new Vector3(-18.5f, 1.05f, 4f), new Vector3(0.9f, 1.8f, 0.5f), darkMaterial);
Station(power, BaseStation.UseKind.Power, "переключить питание базы", 0.8f);
var inverter = Box("Solar inverter", new Vector3(-22f, 0.7f, 1f), new Vector3(1.1f, 1.3f, 0.9f), darkMaterial);
Station(inverter, BaseStation.UseKind.Solar, "переключить зарядку солнечных панелей", 0.8f);
}
}