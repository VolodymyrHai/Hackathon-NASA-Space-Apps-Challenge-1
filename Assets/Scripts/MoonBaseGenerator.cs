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
        if (terrain == null) terrain = GetComponent<MoonWorldGenerator>();
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
        if (terrain == null) terrain = GetComponent<MoonWorldGenerator>();
        if (terrain == null) return;
        Clear();
        generated = new GameObject("Main Moon Base (generated)");
        generated.hideFlags = HideFlags.DontSave;
        generated.transform.SetParent(terrain.transform, false);
        float ground = terrain.flattenBaseSite ? terrain.baseHeight : terrain.SampleHeightLocal(terrain.baseCenter.x, terrain.baseCenter.y);
        generated.transform.localPosition = new Vector3(terrain.baseCenter.x, ground, terrain.baseCenter.y);
        lastCenter = terrain.baseCenter; lastHeight = terrain.baseHeight; dirty = false;
        services = generated.AddComponent<MoonBaseServices>();
        services.Initialize(terrain.player, inventory);

        // Low plaza lip remains below the player's step height.
        Box("Foundation plaza", new Vector3(0,-.10f,0), new Vector3(48,.36f,54), darkMaterial);
        var command = Module("Command and communications", new Vector3(0,0,18), 12, 10);
        Station(command, BaseStation.UseKind.Command, "состояние базы", .5f);
        Module("Habitat A", new Vector3(-14,0,5), 10, 9);
        Module("Habitat B", new Vector3(14,0,5), 10, 9);
        Module("Workshop and storage", new Vector3(-15,0,-16), 11, 9);
        // Open connecting walkways: entrances face the plaza, with no door collider.
        Walkway("Command access", new Vector3(0,.10f,6), new Vector3(3,.12f,14));
        Walkway("West access", new Vector3(-8,.10f,-1), new Vector3(16,.12f,3));
        Walkway("East access", new Vector3(8,.10f,-1), new Vector3(16,.12f,3));
        LandingPad(new Vector3(15,0,-17));
        SolarFarm();
        Antenna(new Vector3(24,0,23));
        Utilities();
        FunctionalInteriors();
        // A clear southern approach from the existing player spawn.
        Box("South approach",new Vector3(0,-.015f,-29),new Vector3(5,.15f,6),darkMaterial);
#if UNITY_EDITOR
        if (!Application.isPlaying) SceneView.RepaintAll();
#endif
        if (Application.isPlaying) Physics.SyncTransforms();
    }
    GameObject Part(PrimitiveType type, string title, Vector3 position, Vector3 scale, Material material, bool solid = true)
    {
        var obj = GameObject.CreatePrimitive(type);
        obj.name = title; obj.hideFlags = HideFlags.DontSave; obj.layer = gameObject.layer;
        obj.transform.SetParent(generated.transform, false);
        obj.transform.localPosition = position; obj.transform.localScale = scale;
        obj.GetComponent<Renderer>().sharedMaterial = material;
        var collider = obj.GetComponent<Collider>();
        if (!solid && collider != null) collider.enabled = false;
        return obj;
    }
    GameObject Box(string title,Vector3 p,Vector3 size,Material material,bool solid=true)
    { return Part(PrimitiveType.Cube,title,p,size,material,solid); }
    void Walkway(string title,Vector3 p,Vector3 size)
    {
        Box(title,p,size,darkMaterial);
        Box(title+" edge L",p+new Vector3(-size.x*.5f+.06f,.07f,0),new Vector3(.12f,.025f,size.z),accentMaterial,false);
        Box(title+" edge R",p+new Vector3(size.x*.5f-.06f,.07f,0),new Vector3(.12f,.025f,size.z),accentMaterial,false);
    }
    GameObject Module(string title,Vector3 center,float width,float depth)
    {
        const float h=3.8f, wall=.22f, door=2.8f;
        Box(title+" floor",center+new Vector3(0,.12f,0),new Vector3(width,.16f,depth),darkMaterial);
        Box(title+" roof",center+new Vector3(0,h,0),new Vector3(width+.4f,.26f,depth+.4f),structureMaterial);
        Box(title+" back",center+new Vector3(0,h*.5f,depth*.5f),new Vector3(width,h,wall),structureMaterial);
        Box(title+" left",center+new Vector3(-width*.5f,h*.5f,0),new Vector3(wall,h,depth),structureMaterial);
        Box(title+" right",center+new Vector3(width*.5f,h*.5f,0),new Vector3(wall,h,depth),structureMaterial);
        float wing=(width-door)*.5f;
        foreach(float side in new[]{-1f,1f})
        {
            Box(title+" doorway side",center+new Vector3(side*(door+wing)*.5f,h*.5f,-depth*.5f),new Vector3(wing,h,wall),structureMaterial);
            Box(title+" entrance marker",center+new Vector3(side*(door*.5f+.1f),1.4f,-depth*.5f-.15f),new Vector3(.13f,2.7f,.12f),accentMaterial,false);
        }
        Box(title+" doorway lintel",center+new Vector3(0,3.3f,-depth*.5f),new Vector3(door,1f,wall),structureMaterial);
        var console = Box(title+" console",center+new Vector3(0,.75f,depth*.5f-1.2f),new Vector3(2.6f,1.2f,.7f),darkMaterial);
        Box(title+" screen",center+new Vector3(0,1.55f,depth*.5f-1.25f),new Vector3(2f,.7f,.10f),solarMaterial,false);
        var doorAnchor = new GameObject(title + " automatic door");
        doorAnchor.hideFlags = HideFlags.DontSave;
        doorAnchor.transform.SetParent(generated.transform,false);
        doorAnchor.transform.localPosition = center + new Vector3(0,.2f,-depth*.5f);
        var left = Box(title+" door L",center+new Vector3(-door*.25f,1.55f,-depth*.5f),new Vector3(door*.5f-.05f,2.7f,.16f),structureMaterial);
        var right = Box(title+" door R",center+new Vector3(door*.25f,1.55f,-depth*.5f),new Vector3(door*.5f-.05f,2.7f,.16f),structureMaterial);
        doorAnchor.AddComponent<BaseDoor>().Configure(terrain.player,services,left.transform,right.transform);
        var lampObject = new GameObject(title+" lamp");lampObject.hideFlags=HideFlags.DontSave;
        lampObject.transform.SetParent(generated.transform,false);lampObject.transform.localPosition=center+new Vector3(0,3.2f,0);
        var lamp=lampObject.AddComponent<Light>();lamp.type=LightType.Point;lamp.color=new Color(.85f,.91f,1f);lamp.range=8;lamp.intensity=1.8f;lamp.shadows=LightShadows.None;services.RegisterLamp(lamp);
        return console;
    }
    void LandingPad(Vector3 p)
    {
        // Render-only disk over the already solid foundation, avoiding a curved CapsuleCollider.
        Part(PrimitiveType.Cylinder,"Landing pad",p+Vector3.up*.105f,new Vector3(15,.025f,15),darkMaterial,false);
        Box("Landing H left",p+new Vector3(-1.2f,.14f,0),new Vector3(.35f,.02f,4),accentMaterial,false);
        Box("Landing H right",p+new Vector3(1.2f,.14f,0),new Vector3(.35f,.02f,4),accentMaterial,false);
        Box("Landing H bridge",p+new Vector3(0,.14f,0),new Vector3(2.4f,.02f,.35f),accentMaterial,false);
    }
    void SolarFarm()
    {
        for(int row=0;row<3;row++)for(int col=0;col<2;col++)
        {
            Vector3 p=new Vector3(-32+col*7,0,8+row*8);
            Box("Solar support",p+Vector3.up*.9f,new Vector3(.25f,1.8f,.25f),darkMaterial);
            var panel=Box("Solar array",p+Vector3.up*2,new Vector3(5,.15f,5.5f),solarMaterial);
            panel.transform.localRotation=Quaternion.Euler(22,0,0);
        }
    }
    void Antenna(Vector3 p)
    {
        Part(PrimitiveType.Cylinder,"Communication mast",p+Vector3.up*4,new Vector3(.45f,4,.45f),darkMaterial);
        var dish=Part(PrimitiveType.Sphere,"Communication dish",p+Vector3.up*8.5f,new Vector3(4,.35f,4),structureMaterial,false);
        dish.transform.localRotation=Quaternion.Euler(35,0,0);
        Box("Antenna equipment",p+new Vector3(2,.7f,0),new Vector3(2,1.4f,2),structureMaterial);
    }
    void Utilities()
    {
        var random=new System.Random(layoutSeed);
        for(int i=0;i<5;i++)
        {
            Vector3 p=new Vector3(-20+i*2.3f,.65f,-23);
            var crate=Box("Supply container "+(i+1),p,new Vector3(1.8f,1.2f,1.6f),structureMaterial);
            crate.transform.localRotation=Quaternion.Euler(0,(float)random.NextDouble()*12-6,0);
            var station = Station(crate,BaseStation.UseKind.Supply,"открыть резервный контейнер",1.2f,i);
            station.usedMarker=Box("Opened container marker",p+Vector3.up*.62f,new Vector3(1.3f,.05f,.4f),accentMaterial,false);
            station.usedMarker.SetActive(inventory.openedCrates[i]);
        }
        for(int i=0;i<3;i++)
        {
            Vector3 p=new Vector3(24,1.7f,-3+i*3);
            Part(PrimitiveType.Cylinder,"Oxygen storage "+(i+1),p,new Vector3(1.6f,1.7f,1.6f),structureMaterial);
        }
    }
    BaseStation Station(GameObject obj, BaseStation.UseKind kind, string label, float seconds, int index=-1)
    {
        var station=obj.AddComponent<BaseStation>();station.Configure(services,kind,label,seconds,index);return station;
    }
    void FunctionalInteriors()
    {
        var water=Part(PrimitiveType.Cylinder,"Water dispenser",new Vector3(-17.2f,1.05f,7),new Vector3(.8f,1.05f,.8f),structureMaterial);
        Station(water,BaseStation.UseKind.Water,"пополнить воду (+40)",1.3f);
        Box("Water dispenser tap",new Vector3(-17.2f,.95f,6.5f),new Vector3(.25f,.12f,.3f),solarMaterial,false);
        var food=Box("Food counter",new Vector3(-11,.8f,7),new Vector3(1.6f,1.4f,1),structureMaterial);
        Station(food,BaseStation.UseKind.Food,"взять рацион (+35)",1.6f);
        var bed=Box("Rest bunk",new Vector3(12,.45f,6),new Vector3(1.8f,.65f,3),darkMaterial);
        Box("Rest mattress",new Vector3(12,.82f,6),new Vector3(1.65f,.12f,2.8f),structureMaterial,false);
        Station(bed,BaseStation.UseKind.Rest,"отдохнуть (+40 к состоянию)",3f);
        var med=Box("Medical station",new Vector3(17.1f,.85f,7),new Vector3(1.4f,1.5f,1),structureMaterial);
        Station(med,BaseStation.UseKind.Medical,"вылечить травмы и переломы",3f);
        Box("Medical cross horizontal",new Vector3(17.1f,1.4f,6.48f),new Vector3(.75f,.16f,.03f),accentMaterial,false);
        Box("Medical cross vertical",new Vector3(17.1f,1.4f,6.47f),new Vector3(.16f,.75f,.03f),accentMaterial,false);
        var power=Box("Base power cabinet",new Vector3(-12.3f,1.1f,-13),new Vector3(.9f,1.9f,.5f),darkMaterial);
        Station(power,BaseStation.UseKind.Power,"переключить питание базы",.8f);
        var inverter=Box("Solar inverter",new Vector3(-25,.7f,2),new Vector3(1.2f,1.4f,1),darkMaterial);
        Station(inverter,BaseStation.UseKind.Solar,"переключить зарядку солнечных панелей",.8f);
    }

}
