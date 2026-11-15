using UnityEngine;
using UnityEngine.UI;

public class SunStorm : MonoBehaviour
{
    [Header("Радіація")]
    public bool affectRadiation = true;
    public int radiationDuringEvent = 3;

    [Header("Вигляд")]
    public Color tintColor = new Color(1f, 0.92f, 0.55f, 1f);
    [Range(0f, 1f)] public float maxAlpha = 0.3f;
    public float pulseSpeed = 3f;
    [Min(0f)] public float fadeInTime = 1.5f;
    [Min(0f)] public float fadeOutTime = 3f;

    public float Intensity { get; private set; }

    private float target;
    private bool radiationActive;
    [SerializeField]private GameObject overlayRoot;
    public static GameObject SunCan;
    private Image overlayImage;

    private void Awake()
    {
        overlayImage = overlayRoot.GetComponentInChildren<Image>();
        overlayImage.raycastTarget = false;
        overlayImage.color = new Color(tintColor.r, tintColor.g, tintColor.b, 0f);
    }

    [ContextMenu("Start Event")]
    public void StartEvent()
    {
        target = 1f;

        if (affectRadiation && !radiationActive)
        {
            radiationActive = true;
            StaticBarsLogic.Radiation(radiationDuringEvent);
        }
    }

    [ContextMenu("Stop Event")]
    public void StopEvent()
    {
        target = 0f;

        if (affectRadiation && radiationActive)
        {
            radiationActive = false;
            StaticBarsLogic.Radiation(0);
        }
    }
    public bool startOnPlay;
    private void Start()
    {
        if (startOnPlay) StartEvent();
    }
    private void Update()
    {
        float duration = target > Intensity ? fadeInTime : fadeOutTime;
        float step = duration <= 0f ? 1f : Time.deltaTime / duration;
        Intensity = Mathf.MoveTowards(Intensity, target, step);

        float pulse = 0.7f + 0.3f * Mathf.Sin(Time.time * pulseSpeed);
        overlayImage.color = new Color(tintColor.r, tintColor.g, tintColor.b, maxAlpha * Intensity * pulse);
    }

    private void OnDestroy()
    {
        if (radiationActive)
        {
            radiationActive = false;
            StaticBarsLogic.Radiation(0);
        }

        if (overlayRoot != null) Destroy(overlayRoot);
    }
}
