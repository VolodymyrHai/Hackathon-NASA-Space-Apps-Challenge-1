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
    public GameObject customBasePrefab;
    public bool previewInEditor = true;
    public int layoutSeed = 42;
    [SerializeField] MoonBaseState inventory = new MoonBaseState();
    MoonBaseServices services;
    GameObject generated;
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

        if (customBasePrefab != null)
        {
            var customBase = Instantiate(customBasePrefab, generated.transform);
            customBase.name = "Custom Moon Base";
            customBase.hideFlags = HideFlags.DontSave;
            customBase.transform.localPosition = Vector3.zero;
            customBase.transform.localRotation = Quaternion.identity;

            var existingStations = customBase.GetComponentsInChildren(typeof(BaseStation), true);
            for (int i = 0; i < existingStations.Length; i++)
            {
                var st = (BaseStation)existingStations[i];
                st.Configure(services, st.kind, st.stationName, st.holdSeconds, st.supplyIndex);
            }

            if (existingStations.Length == 0)
            {
                var cmdZone = Box("Command Zone", new Vector3(0f, 1f, 0f), new Vector3(1.5f, 1.5f, 1.5f), darkMaterial, false);
                ((Renderer)cmdZone.GetComponent(typeof(Renderer))).enabled = false;
                Station(cmdZone, BaseStation.UseKind.Command, "check base status", 0.5f);

                var kitchenZone = Box("Kitchen Zone", new Vector3(-5f, 1f, 0f), new Vector3(1.5f, 1.5f, 1.5f), darkMaterial, false);
                ((Renderer)kitchenZone.GetComponent(typeof(Renderer))).enabled = false;
                Station(kitchenZone, BaseStation.UseKind.Food, "take ration (+35)", 1.5f);

                var waterZone = Box("Water Zone", new Vector3(-5f, 1f, 2f), new Vector3(1.5f, 1.5f, 1.5f), darkMaterial, false);
                ((Renderer)waterZone.GetComponent(typeof(Renderer))).enabled = false;
                Station(waterZone, BaseStation.UseKind.Water, "refill water (+40)", 1.3f);

                var gymZone = Box("Gym Zone", new Vector3(5f, 1f, 0f), new Vector3(1.5f, 1.5f, 1.5f), darkMaterial, false);
                ((Renderer)gymZone.GetComponent(typeof(Renderer))).enabled = false;
                Station(gymZone, BaseStation.UseKind.Workout, "work out (+35 sanity)", 2.5f);
            }

            Utilities();
#if UNITY_EDITOR
            if (!Application.isPlaying) SceneView.RepaintAll();
#endif
            if (Application.isPlaying) Physics.SyncTransforms();
            return;
        }

        // Fallback if no custom prefab is assigned yet
        Box("Placeholder Pad", new Vector3(0f, 0.05f, 0f), new Vector3(10f, 0.1f, 10f), darkMaterial);
#if UNITY_EDITOR
        if (!Application.isPlaying) SceneView.RepaintAll();
#endif
        if (Application.isPlaying) Physics.SyncTransforms();
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
        if (!solid && collider != null) collider.enabled = false;
        return obj;
    }

    GameObject Box(string title, Vector3 p, Vector3 size, Material material, bool solid = true)
    {
        return Part(PrimitiveType.Cube, title, p, size, material, solid);
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
    }

    BaseStation Station(GameObject obj, BaseStation.UseKind kind, string label, float seconds, int index = -1)
    {
        var station = (BaseStation)obj.AddComponent(typeof(BaseStation));
        station.Configure(services, kind, label, seconds, index);
        return station;
    }
}
