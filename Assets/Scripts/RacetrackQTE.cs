using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using Unity.VisualScripting;

public class RacetrackQTE : MonoBehaviour
{
    [Header("Canvas")]
    [SerializeField] private GameObject qteCanvas;
    [SerializeField] private GameObject hintCanvas;

    [Header("QTE Bar")]
    [SerializeField] private RectTransform zone;
    [SerializeField] private Image qteFill;

    [Header("Injury Bar")]
    [SerializeField] private Image injuryFill;
    [SerializeField] private RectTransform yellowMark;
    [SerializeField] private RectTransform redMark;
    [SerializeField] private int yellowDamage = 5;
    [SerializeField] private int redDamage = 15;

    [Header("Zone")]
    [SerializeField] private float zoneSpeed = 1.2f;

    [Header("Player fill")]
    [SerializeField] private float fillSpeed = 0.8f;
    [SerializeField] private float drainSpeed = 0.6f;
    [SerializeField] private float smoothTime = 0.06f;

    [Header("Injury")]
    [SerializeField] private float injuryPerSecond = 0.15f;

    [Header("Timing")]
    [SerializeField] private float inframeTime = 2f;
    [SerializeField] private float duration = 15f;

    private string playerTag = "Player";
    private readonly Vector3[] corners = new Vector3[4];
    private bool qteActive;
    private bool active;
    private float fillVelocity;
    private float targetFill;
    private float yellowThreshold;
    private float redThreshold;
    private float timer;
    private float startTime;
    private bool isPlayer;

    private void Start()
    {
        qteCanvas.SetActive(true);
        Canvas.ForceUpdateCanvases();
        yellowThreshold = GetMarkFraction(yellowMark);
        redThreshold = GetMarkFraction(redMark);
        qteCanvas.SetActive(false);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(playerTag))
        {
            hintCanvas.SetActive(true);
            isPlayer = true;
        }
    }

    private void OnTriggerStay(Collider other)
    {
        if(other.CompareTag(playerTag) && qteActive)
        {
            hintCanvas.SetActive(false);
        }
    }
    private void OnTriggerExit(Collider other)
    {
        isPlayer = false;
        hintCanvas.SetActive(false);
    }
    private void Update()
    {
        if (!active)
        {
            if (isPlayer && Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
                StartQTE();
            return;
        }

        timer += Time.deltaTime;

        MoveZone();
        MovePlayerFill();
        CheckInjury();

        if (active && timer >= duration)
            StopQTE();
    }

    private void StartQTE()
    {
        qteActive = true;
        qteCanvas.SetActive(true);
        Canvas.ForceUpdateCanvases();

        timer = 0f;
        startTime = Time.time;
        fillVelocity = 0f;
        targetFill = 0f;

        SetupFilled(qteFill);
        SetupFilled(injuryFill);

        active = true;
    }

    private void StopQTE()
    {
        active = false;
        qteCanvas.SetActive(false);
    }

    private void SetupFilled(Image image)
    {
        image.type = Image.Type.Filled;
        image.fillMethod = Image.FillMethod.Horizontal;
        image.fillOrigin = (int)Image.OriginHorizontal.Left;
        image.fillAmount = 0f;
    }

    private bool IsPressed()
    {
        bool mouse = Mouse.current != null && Mouse.current.leftButton.isPressed;
        bool space = Keyboard.current != null && Keyboard.current.spaceKey.isPressed;
        return mouse || space;
    }

    private void GetXRange(RectTransform rect, out float left, out float right)
    {
        rect.GetWorldCorners(corners);
        left = corners[0].x;
        right = corners[2].x;
    }

    private float GetMarkFraction(RectTransform mark)
    {
        GetXRange(injuryFill.rectTransform, out float left, out float right);
        return Mathf.Clamp01(Mathf.InverseLerp(left, right, mark.position.x));
    }

    private void MoveZone()
    {
        GetXRange(qteFill.rectTransform, out float barLeft, out float barRight);
        GetXRange(zone, out float zoneLeft, out float zoneRight);

        float halfWidth = (zoneRight - zoneLeft) * 0.5f;
        float currentCenter = (zoneLeft + zoneRight) * 0.5f;

        float t = (Mathf.Sin((Time.time - startTime) * zoneSpeed) + 1f) * 0.5f;
        float targetCenter = Mathf.Lerp(barLeft + halfWidth, barRight - halfWidth, t);

        Vector3 position = zone.position;
        position.x += targetCenter - currentCenter;
        zone.position = position;
    }

    private void MovePlayerFill()
    {
        targetFill += (IsPressed() ? fillSpeed : -drainSpeed) * Time.deltaTime;
        targetFill = Mathf.Clamp01(targetFill);

        qteFill.fillAmount = Mathf.SmoothDamp(qteFill.fillAmount, targetFill, ref fillVelocity, smoothTime);
    }

    private bool IsFillInZone()
    {
        GetXRange(qteFill.rectTransform, out float barLeft, out float barRight);
        GetXRange(zone, out float zoneLeft, out float zoneRight);

        float fillEdge = Mathf.Lerp(barLeft, barRight, qteFill.fillAmount);
        return fillEdge >= zoneLeft && fillEdge <= zoneRight;
    }

    private void CheckInjury()
    {
        if (timer < inframeTime) return;
        if (IsFillInZone()) return;

        float previous = injuryFill.fillAmount;
        float current = Mathf.Min(1f, previous + injuryPerSecond * Time.deltaTime);
        injuryFill.fillAmount = current;

        if (previous < yellowThreshold && current >= yellowThreshold)
            DamageRandomLeg(yellowDamage);

        if (previous < redThreshold && current >= redThreshold)
            DamageRandomLeg(redDamage);

        if (current >= 1f)
            StopQTE();
    }

    private void DamageRandomLeg(int damage)
    {
        BodyPartType leg = Random.value < 0.5f ? BodyPartType.leftLeg : BodyPartType.rigthLeg;
        StaticBarsLogic.Damage(leg, damage);
    }
}