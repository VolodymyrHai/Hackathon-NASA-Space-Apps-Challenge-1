using System.Collections;
using TMPro;
using UnityEngine;

public class GameTimeController : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private TextMeshProUGUI timeText;
    [SerializeField] private TextMeshProUGUI dayText;

    [Header("Time Settings")]
    [SerializeField] private float dayLengthInMinutes = 20f;
    [SerializeField] private float startHour = 6.00f;

    private float currentTimeInSeconds;
    private float timeMultiplier;

    public int CurrentDay { get; private set; } = 1;
    public int CurrentHour { get; private set; }
    public int CurrentMinute { get; private set; }
    private void Start()
    {
        float realDayLengthInSeconds = dayLengthInMinutes * 60f;
        timeMultiplier = 86400f / realDayLengthInSeconds;
        currentTimeInSeconds = startHour * 3600f;
        StartCoroutine(TimeCourutine());
    }
    private void Update()
    {
        timeText.text = $"{CurrentHour:D2}:{CurrentMinute:D2}";
        dayText.text = $"Day: {CurrentDay}";
    }
    private IEnumerator TimeCourutine()
    {
        while (true)
        {
            currentTimeInSeconds += Time.deltaTime * timeMultiplier;
            float totalHours = currentTimeInSeconds / 3600f;
            CurrentDay = Mathf.FloorToInt(totalHours / 24f) + 1;
            CurrentHour = Mathf.FloorToInt(totalHours) % 24;
            float fracHours = totalHours - Mathf.Floor(totalHours);
            CurrentMinute = Mathf.FloorToInt(fracHours * 60f);
            yield return null;
        }
    }
}