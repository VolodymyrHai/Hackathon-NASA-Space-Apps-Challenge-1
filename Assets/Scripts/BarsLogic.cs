using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

public class BarsLogic : MonoBehaviour
{
    [SerializeField] private Image thirstBar;
    [SerializeField] private Image hungerBar;
    [SerializeField] private Image mindBar;

    [Header("Water")]
    [SerializeField] private float thirstInterval = 5f;
    [SerializeField, Range(0f, 100f)] private float thirstDrain = 2f;
    [Header("Food")]
    [SerializeField] private float hungerInterval = 5f;
    [SerializeField, Range(0f, 100f)] private float hungerDrain = 1f;

    [Header("Sanity")]
    [SerializeField, Range(0f, 100f)] private float startMind = 100f;

    [Header("Events when bar reaches zero")]
    public UnityEvent onThirstEmpty;
    public UnityEvent onHungerEmpty;
    public UnityEvent onMindEmpty;

    public float ThirstPercent => thirst;
    public float HungerPercent => hunger;
    public float MindPercent => mind;

    private float thirst = 100f;
    private float hunger = 100f;
    private float mind;

    private float thirstTimer;
    private float hungerTimer;
    void Awake()
    {
        StaticBarsLogic.bars = this;
    }
    private void Start()
    {
        mind = Mathf.Clamp(startMind,0f,100f);
        UpdateUI();
    }

    private void Update()
    {
        thirstTimer += Time.deltaTime;
        float waterPeriod = Mathf.Max(.1f, thirstInterval);
        if (thirstTimer >= waterPeriod)
        {
            int ticks = Mathf.FloorToInt(thirstTimer / waterPeriod);
            thirstTimer -= ticks * waterPeriod;
            ChangeThirst(-Mathf.Max(0f, thirstDrain) * ticks);
        }

        hungerTimer += Time.deltaTime;
        float foodPeriod = Mathf.Max(.1f, hungerInterval);
        if (hungerTimer >= foodPeriod)
        {
            int ticks = Mathf.FloorToInt(hungerTimer / foodPeriod);
            hungerTimer -= ticks * foodPeriod;
            ChangeHunger(-Mathf.Max(0f, hungerDrain) * ticks);
        }
    }


    public void ChangeThirst(float amount)
    {
        bool wasAlive = thirst > 0f;
        thirst = Mathf.Clamp(thirst + amount, 0f, 100f);
        if (wasAlive && thirst <= 0f) onThirstEmpty?.Invoke();
        UpdateUI();
    }

    public void ChangeHunger(float amount)
    {
        bool wasAlive = hunger > 0f;
        hunger = Mathf.Clamp(hunger + amount, 0f, 100f);
        if (wasAlive && hunger <= 0f) onHungerEmpty?.Invoke();
        UpdateUI();
    }

    public void ChangeMind(float amount)
    {
        bool wasAlive = mind > 0f;
        mind = Mathf.Clamp(mind + amount, 0f, 100f);
        if (wasAlive && mind <= 0f) onMindEmpty?.Invoke();
        UpdateUI();
    }

    private void UpdateUI()
    {
        if (thirstBar) thirstBar.fillAmount = thirst / 100f;
        if (hungerBar) hungerBar.fillAmount = hunger / 100f;
        if (mindBar) mindBar.fillAmount = mind / 100f;
    }
}