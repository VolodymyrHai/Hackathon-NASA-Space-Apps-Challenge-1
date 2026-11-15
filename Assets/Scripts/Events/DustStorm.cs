using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Візуал пилової бурі: туман + частинки пилу + екранний фільтр.
/// Усе створюється з коду. Викликайте StartStorm() / StopStorm().
/// Плавно наростає й згасає.
///
/// З іншого скрипта:
///   DustStorm.Instance.StartStorm();
///   DustStorm.Instance.StopStorm();         // плавно
///   DustStorm.Instance.StopStormInstant();  // миттєво
///   DustStorm.Instance.SetStorm(true/false);
///   DustStorm.Instance.ToggleStorm();
///   DustStorm.Instance.IsActive;
/// </summary>
public class DustStorm : MonoBehaviour
{
    [Header("Керування")]
    public bool startOnPlay = false;
    [Min(0f)] public float fadeInTime = 4f;
    [Min(0f)] public float fadeOutTime = 6f;

    [Header("Вигляд")]
    public Color dustColor = new Color(0.80f, 0.50f, 0.25f, 1f);
    public Vector3 windDirection = new Vector3(1f, 0.05f, 0.3f);
    public float windSpeed = 30f;

    [Header("Туман (3D)")]
    public bool useFog = true;
    public float fogDensity = 0.05f;

    [Header("Частинки")]
    [Tooltip("Великі м'які хмари пилу")]
    public float cloudsPerSecond = 40f;
    [Tooltip("Швидкі смуги, що створюють відчуття вітру")]
    public float streaksPerSecond = 150f;
    public int sortingOrder = 50;

    [Header("Екранний фільтр")]
    [Range(0f, 1f)] public float overlayAlpha = 0.25f;

    [Header("Сонце (необов'язково)")]
    public Light sun;
    [Range(0f, 1f)] public float sunIntensityMultiplier = 0.4f;

    [Header("Радіація")]
    public bool affectRadiation = true;
    [Tooltip("Значення, яке передається в StaticBarsLogic.Radiation() під час бурі")]
    public int radiationDuringStorm = 2;

    /// <summary>Доступ до бурі з будь-якого скрипта (використовуйте у Start(), а не в Awake()).</summary>
    public static DustStorm Instance { get; private set; }

    /// <summary>Сила бурі 0..1. Можна використати в геймплеї (сповільнення, видимість тощо).</summary>
    public float Intensity { get; private set; }

    /// <summary>true, поки буря триває або ще не повністю згасла.</summary>
    public bool IsActive => target > 0f || Intensity > 0f;

    /// <summary>Чи показується візуал бурі. Не впливає на радіацію й Intensity.</summary>
    public bool VisualsVisible { get; private set; } = true;

    private float target;
    private bool radiationActive;

    // оригінальні налаштування сцени
    private bool origFog;
    private Color origFogColor;
    private FogMode origFogMode;
    private float origFogDensity;
    private float origSunIntensity;
    private Color origSunColor;
    private bool fogApplied;

    // згенеровані об'єкти
    private Texture2D softTexture;
    private Material particleMaterial;
    private GameObject particlesRoot, overlayRoot;
    private ParticleSystem clouds, streaks;
    private Image overlayImage;
    private Camera cam;

    private void Awake()
    {
        Instance = this;

        origFog = RenderSettings.fog;
        origFogColor = RenderSettings.fogColor;
        origFogMode = RenderSettings.fogMode;
        origFogDensity = RenderSettings.fogDensity;

        if (sun != null)
        {
            origSunIntensity = sun.intensity;
            origSunColor = sun.color;
        }

        BuildVisuals();
    }

    private void Start()
    {
        if (startOnPlay) StartStorm();
    }

    // ───────────── Керування ─────────────

    [ContextMenu("Start Storm")]
    public void StartStorm()
    {
        target = 1f;

        if (affectRadiation && !radiationActive)
        {
            radiationActive = true;
            StaticBarsLogic.Radiation(radiationDuringStorm);
        }
    }

    /// <summary>Плавно вимикає бурю (згасає за fadeOutTime).</summary>
    [ContextMenu("Stop Storm")]
    public void StopStorm()
    {
        target = 0f;

        if (affectRadiation && radiationActive)
        {
            radiationActive = false;
            StaticBarsLogic.Radiation(0);
        }
    }

    /// <summary>Вмикає або вимикає бурю (плавно).</summary>
    public void SetStorm(bool on)
    {
        if (on) StartStorm();
        else StopStorm();
    }

    /// <summary>Перемикає стан бурі.</summary>
    public void ToggleStorm()
    {
        SetStorm(target <= 0f);
    }

    /// <summary>Вимикає бурю миттєво, без плавного згасання.</summary>
    [ContextMenu("Stop Storm Instantly")]
    public void StopStormInstant()
    {
        StopStorm();       // target = 0 і скидання радіації
        Intensity = 0f;
        Apply();           // повертає туман, сонце, фільтр
        clouds.Clear();    // прибирає частинки, що вже летять
        streaks.Clear();
    }

    /// <summary>Показує або ховає лише візуал (туман, частинки, фільтр, сонце). Логіка бурі й радіація не змінюються.</summary>
    public void SetVisuals(bool visible)
    {
        VisualsVisible = visible;

        if (!visible)
        {
            clouds.Clear();   // прибрати частинки, що вже летять
            streaks.Clear();
        }

        Apply();
    }

    // ───────────── Оновлення ─────────────

    private void Update()
    {
        float duration = target > Intensity ? fadeInTime : fadeOutTime;
        float step = duration <= 0f ? 1f : Time.deltaTime / duration;
        Intensity = Mathf.MoveTowards(Intensity, target, step);

        Apply();
    }

    private void LateUpdate()
    {
        if (Intensity <= 0f) return;

        if (cam == null) cam = Camera.main;
        if (cam == null) return;

        Vector3 wind = windDirection.sqrMagnitude > 0.001f ? windDirection.normalized : Vector3.right;

        // система стоїть "проти вітру" від камери, частинки летять повз неї
        particlesRoot.transform.SetPositionAndRotation(
            cam.transform.position - wind * 20f,
            Quaternion.LookRotation(wind));
    }

    // ───────────── Застосування ефектів ─────────────

    private void Apply()
    {
        float t = VisualsVisible ? Intensity : 0f;

        // туман
        if (useFog)
        {
            if (t > 0f)
            {
                fogApplied = true;
                RenderSettings.fog = true;
                RenderSettings.fogMode = FogMode.ExponentialSquared;
                RenderSettings.fogColor = Color.Lerp(origFogColor, dustColor, t);
                float startDensity = origFog ? origFogDensity : 0f;
                RenderSettings.fogDensity = Mathf.Lerp(startDensity, fogDensity, t);
            }
            else if (fogApplied)
            {
                RestoreFog();
            }
        }

        // сонце
        if (sun != null)
        {
            sun.intensity = Mathf.Lerp(origSunIntensity, origSunIntensity * sunIntensityMultiplier, t);
            sun.color = Color.Lerp(origSunColor, dustColor, t * 0.5f);
        }

        // екранний фільтр
        overlayImage.color = new Color(dustColor.r, dustColor.g, dustColor.b, overlayAlpha * t);

        // частинки
        UpdateSystem(clouds, cloudsPerSecond * t, t);
        UpdateSystem(streaks, streaksPerSecond * t, t);
    }

    private static void UpdateSystem(ParticleSystem ps, float rate, float t)
    {
        var emission = ps.emission;
        emission.rateOverTime = rate;

        if (t > 0f && !ps.isPlaying) ps.Play();
        else if (t <= 0f && ps.isPlaying) ps.Stop(true, ParticleSystemStopBehavior.StopEmitting);
    }

    private void RestoreFog()
    {
        fogApplied = false;
        RenderSettings.fog = origFog;
        RenderSettings.fogColor = origFogColor;
        RenderSettings.fogMode = origFogMode;
        RenderSettings.fogDensity = origFogDensity;
    }

    // ───────────── Генерація візуалу ─────────────

    private void BuildVisuals()
    {
        softTexture = MakeSoftTexture(64);

        // Sprites/Default є у всіх проєктах (Built-in і URP)
        particleMaterial = new Material(Shader.Find("Sprites/Default"));
        particleMaterial.mainTexture = softTexture;

        particlesRoot = new GameObject("DustStorm_Particles");

        Color cloudColor = new Color(dustColor.r, dustColor.g, dustColor.b, 0.12f);
        Color streakColor = new Color(
            Mathf.Lerp(dustColor.r, 1f, 0.3f),
            Mathf.Lerp(dustColor.g, 1f, 0.3f),
            Mathf.Lerp(dustColor.b, 1f, 0.3f), 0.35f);

        clouds = CreateSystem("Clouds", new Vector2(6f, 14f), new Vector2(4f, 7f),
            new Vector2(windSpeed * 0.5f, windSpeed * 0.8f), cloudColor, false);

        streaks = CreateSystem("Streaks", new Vector2(0.2f, 0.5f), new Vector2(1.5f, 2.5f),
            new Vector2(windSpeed * 1.2f, windSpeed * 1.8f), streakColor, true);

        // екранний фільтр
        overlayRoot = new GameObject("DustOverlay", typeof(Canvas));
        var canvas = overlayRoot.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 50;

        var tint = new GameObject("Tint", typeof(RectTransform), typeof(Image));
        tint.transform.SetParent(overlayRoot.transform, false);
        var rt = tint.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;

        overlayImage = tint.GetComponent<Image>();
        overlayImage.raycastTarget = false;
        overlayImage.color = new Color(dustColor.r, dustColor.g, dustColor.b, 0f);
    }

    private ParticleSystem CreateSystem(string name, Vector2 size, Vector2 lifetime,
                                        Vector2 speed, Color color, bool streak)
    {
        var go = new GameObject(name);
        go.transform.SetParent(particlesRoot.transform, false);

        var ps = go.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        var main = ps.main;
        main.playOnAwake = false;
        main.loop = true;
        main.duration = 5f;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.startLifetime = new ParticleSystem.MinMaxCurve(lifetime.x, lifetime.y);
        main.startSpeed = new ParticleSystem.MinMaxCurve(speed.x, speed.y);
        main.startSize = new ParticleSystem.MinMaxCurve(size.x, size.y);
        main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
        main.startColor = color;
        main.maxParticles = 3000;

        var emission = ps.emission;
        emission.rateOverTime = 0f;

        // плоский "екран" перед камерою, з якого летять частинки (по локальній осі Z = напрямок вітру)
        var shape = ps.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = new Vector3(80f, 40f, 30f);

        // плавна поява й зникнення
        var col = ps.colorOverLifetime;
        col.enabled = true;
        var gradient = new Gradient();
        gradient.SetKeys(
            new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
            new[]
            {
                new GradientAlphaKey(0f, 0f),
                new GradientAlphaKey(1f, 0.2f),
                new GradientAlphaKey(1f, 0.8f),
                new GradientAlphaKey(0f, 1f)
            });
        col.color = gradient;

        // турбулентність
        var noise = ps.noise;
        noise.enabled = true;
        noise.strength = streak ? 0.5f : 3f;
        noise.frequency = 0.15f;
        noise.scrollSpeed = 0.5f;

        var r = go.GetComponent<ParticleSystemRenderer>();
        r.sharedMaterial = particleMaterial;
        r.sortingOrder = sortingOrder;
        r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        r.receiveShadows = false;

        if (streak)
        {
            r.renderMode = ParticleSystemRenderMode.Stretch;
            r.lengthScale = 4f;
            r.velocityScale = 0.05f;
        }
        else
        {
            r.renderMode = ParticleSystemRenderMode.Billboard;
        }

        return ps;
    }

    private static Texture2D MakeSoftTexture(int size)
    {
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = (x + 0.5f) / size * 2f - 1f;
                float dy = (y + 0.5f) / size * 2f - 1f;
                float d = Mathf.Sqrt(dx * dx + dy * dy);

                float a = Mathf.Clamp01(1f - d);
                a = a * a * (3f - 2f * a); // smoothstep
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
            }
        }

        tex.Apply();
        return tex;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;

        if (radiationActive)
        {
            radiationActive = false;
            StaticBarsLogic.Radiation(0);
        }

        if (fogApplied) RestoreFog();
        if (sun != null)
        {
            sun.intensity = origSunIntensity;
            sun.color = origSunColor;
        }

        if (particlesRoot != null) Destroy(particlesRoot);
        if (overlayRoot != null) Destroy(overlayRoot);
        if (particleMaterial != null) Destroy(particleMaterial);
        if (softTexture != null) Destroy(softTexture);
    }
}